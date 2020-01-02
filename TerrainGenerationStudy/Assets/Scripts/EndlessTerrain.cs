using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatch to game object
public class EndlessTerrain : MonoBehaviour {
    // store info for diffrent detail levels
    public LODInfo[] detailLevels;

    // player properties
    public static float maxViewDst;
    public Transform viewer;
    public static Vector2 viewerPosition;
    public Vector2 viewerPositionOld;
    public const float viewerMoveThresholdForChunkUpdate = 25.0f;
    public const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

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
        // set max view distance = to last detail level distance
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;

        // locate map generator
        mapGenerator = FindObjectOfType<MapGenerator>();

        // get chunk size from map generator class
        chunkSize = MapGenerator.mapChunkSize - 1;

        // calculate how many chunks player can see
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        // make sure chunks are updated upon entering play mode
        UpdateVisibleChunks();
    }

    // main update loop, runs every frame
    void Update() {
        // update position of player
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // update chunks after player has moved a certain distance
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
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
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
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

        // keep track of detail levels and corresponding info and meshes
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        // to store map data when recieved and bool to check if recieved
        MapData mapData;
        bool mapDataReceived;

        // keep track of old LOD index, don't have to update if current is the same
        int previousLODIndex = -1;

        // constructor
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

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

            // create new array of meshes for each level of detail
            lodMeshes = new LODMesh[detailLevels.Length];

            // loop through mesh array
            for (int i = 0; i < detailLevels.Length; i++) {
                // create new level of detail mesh
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            // get map data from map generator
            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        // map data callback
        void OnMapDataRecieved(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            // get texture of map from map data and put onto mesh
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            // update terrain chunk
            UpdateTerrainChunk();
        }

        // make terrain chunk update itself
        public void UpdateTerrainChunk() {
            // check if map data has been recieved
            if (mapDataReceived) {
                // find smallest square distance between viewer and AABB
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                // determine if chunk is visible based on viewer distance
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                // determine which detail level should be displayed
                if (visible) {
                    int lodIndex = 0;

                    // loop through detail levels
                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        // lower level of detail if distance is larger than threshold of more detailed mesh
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                            lodIndex = i + 1;
                        }
                        // escape loop if correct level of detail
                        else {
                            break;
                        }
                    }

                    // update mesh with correct level of detail
                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];

                        // if already has mesh update to lod mesh
                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        // request mesh if not yet requested
                        else if (!lodMesh.hasRequestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                // set chunk visibility
                SetVisible(visible);
            }
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

    // fetches own mesh from map generator
    class LODMesh {
        // mesh and properites
        public Mesh mesh;
        int lod;

        // callback to terrain chunk update
        System.Action updateCallback;

        // mesh status
        public bool hasRequestedMesh;
        public bool hasMesh;

        // constructor
        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        // mesh data callback
        public void OnMeshDataRecieved(MeshData meshData) {
            // create mesh with recieved mesh data
            mesh = meshData.CreateMesh();

            // now has a mesh
            hasMesh = true;

            // calls update terrain chunk method
            updateCallback();
        }

        // tell class to request mesh
        public void RequestMesh(MapData mapData) {
            // have requested mesh by calling this function
            hasRequestedMesh = true;

            // request mesh data
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }

    // properties of different levels of detail, shows in inspector
    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDstThreshold;
    }
}

