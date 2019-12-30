using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class MeshGenerator {
    // create mesh for terrain using a given height map
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail) {
		// find height map dimensions
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		// make top corner coordinates halved for centering (left needs to be negative)
		float topLeftX = (width - 1) / -2.0f;
		float topLeftZ = (height - 1) / 2.0f;

        // increment for level of detail (protect against LOD = 0 stopping for loops)
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        // store number of verticies in each line
        int verticesPerLine = ((width - 1) / meshSimplificationIncrement) + 1;

        // create mesh data for terrain mesh
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);

		// keep track of indicies
		int vertexIndex = 0;

        // loop through height map
        for (int y = 0; y < height; y += meshSimplificationIncrement) {
            for (int x = 0; x < width; x += meshSimplificationIncrement) {
                // set current vertex coordinates according to height map
				meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);

				// calculate uvs (find heights in relation to rest of map, percentage between 0 and 1)
				meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                // ignore right and bottom edge verticies of map
                if (x < width - 1 && y < height - 1) {
					// add first triangle of square
					meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
					// add second triangle of square
					meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
				}

                // keep track of location in vertex array
				vertexIndex++;
			}
		}

		return meshData;
	}
}

// stores data of mesh
public class MeshData {
	// store vertices' locations and triangle vetices
	public Vector3[] vertices;
	public int[] triangles;

    // store uvs to support adding textures
	public Vector2[] uvs;

	// keep track of indicies
	int triangleIndex;

	// class constructor
	public MeshData(int meshWidth, int meshHeight) {
		// calculate array sizes
		vertices = new Vector3[meshWidth * meshHeight];
		uvs = new Vector2[meshWidth * meshHeight];
		triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}

    // add triangle created by vertices a, b, and c
    public void AddTriangle(int a, int b, int c) {
        // add vertices of new triangle to triangles array
		triangles[triangleIndex] = a;
		triangles[triangleIndex + 1] = b;
		triangles[triangleIndex + 2] = c;

        // move index of array past the newly added values
		triangleIndex += 3;
	}

    // get mesh from mesh data
    public Mesh CreateMesh() {
        // create new mesh
		Mesh mesh = new Mesh();

        // set properties of mesh uding given mesh data
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

		// fix lighting
		mesh.RecalculateNormals();

		return mesh;
	}
}