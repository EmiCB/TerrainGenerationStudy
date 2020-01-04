using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

// attatch to game object
public class MapGenerator : MonoBehaviour {
    // easily swap what type of map to display
    public enum DrawMode {NoiseMap, ColorMap, Mesh, FalloffMap};
    public DrawMode drawMode;

    // get normalize mode
    public Noise.NormalizeMode normalizeMode;

    // size for each mesh chunk (was 241, but subtracted 2 since border adds 2)
    public const int mapChunkSize = 239;

    // defines how many vertices to account for
    [Range(0,6)]                //make level of detail value into a slider
    public int editorPreviewLOD;

    // noise properties
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]                //make persistence value into a slider
    public float persistence;
    public float lacunarity;

    // allows for scrolling around the map
    public Vector2 offset;

    // mesh settings
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    // generation settings
    public bool useFalloff;

    // editor settings
    public bool autoUpdate;

    // list of terrain types
    public TerrainTypes[] regions;

    // store falloff map
    float[,] falloffMap;

    // queues for threading
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    // runs once before game starts
    void Awake() {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    // checks draw mode and displays corresponding map in editor
    public void DrawMapInEditor() {
        // import map data from map generating function
        MapData mapData = GenerateMapData(Vector2.zero);

        // display map on plane
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // check draw mode and display map accordingly
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    // method to pass into EndlessTerrain for threading, starts MapDataThread
    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        // represents MapDataThread
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
	    };

        // runs MapDataThread
        new Thread(threadStart).Start();
    }

    // threading for map data
    void MapDataThread(Vector2 center, Action<MapData> callback) {
        // get map data
        MapData mapData = GenerateMapData(center);

        // add map data and callback to queue (locked to prevent multiple threads from executing at same time)
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // method to pass into EndlessTerrain for threading, starts MeshDataThread
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        // represents MeshDataThread
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        // runs MeshDataThread
        new Thread(threadStart).Start();
    }

    // threading for map data
    public void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        // make mesh data using giving map data 
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);

        // add mesh data and callback to queue (locked to prevent multiple threads from executing at same time)
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    // main update loop, runs every frame
    void Update() {
        // check if map data queue has something in it
        if (mapDataThreadInfoQueue.Count > 0) {
            // loop through elements in queue
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                // set thread info to next thing in queue
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();

                // call callback with map data
                threadInfo.callback(threadInfo.parameter);
            }
        }

        // check if mesh data queue has something in it
        if (meshDataThreadInfoQueue.Count > 0) {
            // loop through elements in queue
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                // set thread info to next thing in queue
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();

                // call callback with map data
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // create noise map and draw onto plane
    MapData GenerateMapData(Vector2 center) {
        // create noise map using Perlin Noise (add 2 to account for border)
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistence, lacunarity, center + offset, normalizeMode);

        // array of all pixel colors, region colors will be stored to it
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        // loop through noise map to assign regions
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                // check if falloff map is being used
                if (useFalloff) {
                    // subtract falloff from map
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                // get current height of point
                float currentHeight = noiseMap[x, y];

                // loop through regions and assign them to points
                for (int i = 0; i < regions.Length; i++) {
                    // check if height value is within region's height range
                    if (currentHeight >= regions[i].height) {
                        //save new region color to point
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    // break when value is less than region's height
                    else {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    // called whenever a value is changed
    private void OnValidate() {
        // clamp values
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;

        // generate falloff map (in case Awake() has not been called)
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    // hold callbacks and map or mesh data
    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        // constructor
        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

// properties of terrain types based on height of map, shows in inspector
[System.Serializable]
public struct TerrainTypes {
    public string name;
    public float height;
    public Color color;
}

// store noise map and color map information
public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    // constructor
    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}