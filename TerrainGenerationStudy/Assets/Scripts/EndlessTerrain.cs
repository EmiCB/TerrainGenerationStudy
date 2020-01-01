using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatch to game object
public class EndlessTerrain : MonoBehaviour {
    // player properties
    public const float maxViewDst = 450.0f;
    public Transform viewer;
    public static Vector2 viewerPosition;

    // chunk properties
    int chunkSize;
    int chunksVisibleInViewDst;

    public Material mapMaterial;

    // reference to map generator
    static MapGenerator mapGenerator;

    // keep track of all coordinates and chunks to prevent duplicates
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    // keep track of last visible chunks
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    // runs once at the beginning of play mode
    void Start() {
        // locate map generator
        mapGenerator = FindObjectOfType<MapGenerator>();
        // get chunk size from map generator class
        chunkSize = MapGenerator.mapChunkSize - 1;
        // calculate how many chunks player can see
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
    }

    // main update loop, runs every frame
    void Update() {
        // update position of player
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // update chunks
        UpdateVisibleChunks();
    }

    // check player view range and update chunks accordingly
    void UpdateVisibleChunks() {
        // loop through all chunks visible last update
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            // set chunk invisible
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        // clear list
        terrainChunksVisibleLastUpdate.Clear();

        // get coordinate of chunk player is on
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // loop through surrounding chunks
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                // find coordinates of chunks within view distance
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // check if chunk exists, update chunk if it does
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    // add chunk to list of visible chunks from last update
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                // create new chunk if one does not already exist
                else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }

    // represents each terrain chunk
    public class TerrainChunk {
        // mesh and its components
        GameObject meshObject;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        // position (x, z) of chunk in scene
        Vector2 position;

        // AABB, use to find point on perimeter that is closest to another point
        Bounds bounds;

        // constructor
        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material) {
            // find position of chunk
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            // create axis-aligned bounding box for chunk
            bounds = new Bounds(position, Vector2.one * size);

            // instantiate new terrain chunk at new position
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3;

            // set to parent to keep hierarchy nice
            meshObject.transform.parent = parent;

            // chunk invisible by default
            SetVisible(false);

            // get map data from map generator
            mapGenerator.RequestMapData(OnMapDataRecieved);
        }

        // executes when map data is recieved form map generator
        void OnMapDataRecieved(MapData mapData) {
            mapGenerator.RequestMeshData(mapData, OnMeshDataRecieved);
        }

        // executes when mesh data is recieved form map generator
        void OnMeshDataRecieved(MeshData meshData) {
            // create new mesh based on data received
            meshFilter.mesh = meshData.CreateMesh();
        }

        // make terrain chunk update itself
        public void UpdateTerrainChunk() {
            // find smallest square distance between viewer and AABB
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            // determine if chunk is visible based on viewer distance
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            // set chunk visibility
            SetVisible(visible);
        }

        // set chunk visibility
        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        // check chunk visibility status
        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }
}

