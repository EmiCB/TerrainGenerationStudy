using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class MeshGenerator {
    // create mesh for terrain using a given height map
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatshading) {
        // height curve to fix threading issues
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // increment for level of detail (protect against LOD = 0 stopping for loops)
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        // find height map dimensions
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

		// make top corner coordinates halved for centering (left needs to be negative)
		float topLeftX = (meshSizeUnsimplified - 1) / -2.0f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2.0f;

        // store number of verticies in each line
        int verticesPerLine = ((meshSize - 1) / meshSimplificationIncrement) + 1;

        // create mesh data for terrain mesh
        MeshData meshData = new MeshData(verticesPerLine, useFlatshading);

        // keep track of indicies
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        // loop through height map
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x  = 0; x < borderedSize; x += meshSimplificationIncrement) {
                // decide if vertex is on border of mesh
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                // check if current vertex is bordered or not, add to vertex index array, and change the index accordingly
                if (isBorderVertex) {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // loop through height map
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                // get vertex index from vetrex indices map
                int vertexIndex = vertexIndicesMap[x, y];

                // calculate uvs (find heights in relation to rest of map, percentage between 0 and 1)
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);

                // calculate y value for vertex position
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

                // set current vertex coordinates according to height map
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                // add vertex to mesh data
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                // ignore right and bottom edge verticies of map
                if (x < borderedSize - 1 && y < borderedSize - 1) {
                    // find square of vertices for triangles
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

					// add first triangle of square
					meshData.AddTriangle(a, d, c);
					// add second triangle of square
					meshData.AddTriangle(d, a, b);
				}

                // keep track of location in vertex array
				vertexIndex++;
			}
		}

        // handle normals, puts normal calculations in this thread instead of main game
        meshData.ProcessMesh();

		return meshData;
	}
}

// stores data of mesh
public class MeshData {
	// store vertices' locations and triangle vetices
	Vector3[] vertices;
    Vector3[] borderVertices;
	int[] triangles;
    int[] borderTriangles;

    // normals
    Vector3[] bakedNormals;

    // store uvs to support adding textures
	Vector2[] uvs;

	// keep track of indicies
	int triangleIndex;
    int borderTriangleIndex;

    // flatshading options
    bool useFlatshading;

	// class constructor
	public MeshData(int verticesPerLine, bool useFlatshading) {
        this.useFlatshading = useFlatshading;

		// calculate array sizes
		vertices = new Vector3[verticesPerLine * verticesPerLine];
        borderVertices = new Vector3[verticesPerLine * 4 + 4];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        borderTriangles = new int[verticesPerLine * 24];
	}

    // add a vertex
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        // check if it is a border vertex
        if (vertexIndex < 0) {
            // add vertex and uv into arrays (index calculated liek that because border index is negative)
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else {
            // add vertex and uv into arrays
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    // add triangle created by vertices a, b, and c
    public void AddTriangle(int a, int b, int c) {
        // check if border vertex
        if (a < 0 || b < 0 || c < 0) {
            // add vertices of new triangle to triangles array
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;

            // move index of array past the newly added values
            borderTriangleIndex += 3;
        }
        else {
            // add vertices of new triangle to triangles array
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            // move index of array past the newly added values
            triangleIndex += 3;
        }
	}

    // custom method to calculate vertex normals (fixes lighting on seams)
    Vector3[] CalculateNormals() {
        // store vertex normals and find how many triangles there are
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        int borderTriangleCount = borderTriangles.Length / 3;

        // loop through triangles
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;

            // find indices of vertices that make up current triangle
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            // calculate triangle normal from given vertices
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            // add normal to each vertex that is part of that triangle
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        // loop through border triangles
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;

            // find indices of vertices that make up current triangle
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            // calculate triangle normal from given vertices
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            // make sure vertex index >= 0 and add normal to each vertex that is part of that triangle
            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            else if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            else if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        // loop through vertext normals
        for (int i = 0; i < vertexNormals.Length; i++) {
            // normalize each value
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    // returns normal vector of triangle given vertices
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        // find coordinates of points (check if border or mesh)
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // take crossproduct
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    // finalizes the mesh
    public void ProcessMesh() {
        if (useFlatshading) {
            FlatShading();
        }
        else {
            BakeNormals();
        }
    }

    // calculate normals
    void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    // flatshade terrain
    public void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        // loop through vertices
        for (int i = 0; i < triangles.Length; i++) {
            // update arrays
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        // use new vertices and uvs
        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    // get mesh from mesh data
    public Mesh CreateMesh() {
        // create new mesh
		Mesh mesh = new Mesh();

        // set properties of mesh uding given mesh data
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

        // lighting
        if (useFlatshading) {
            mesh.RecalculateNormals();
        }
        else {
            mesh.normals = bakedNormals;
        }

		return mesh;
	}
}