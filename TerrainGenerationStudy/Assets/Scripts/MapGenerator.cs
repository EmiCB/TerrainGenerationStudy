using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatch to game object
public class MapGenerator : MonoBehaviour {
    // map dimensions
    public int mapWidth;
    public int mapHeight;

    // noise properties
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistence;
    public float lacunarity;

    // allows for scrolling around the map
    public Vector2 offset;

    // editor settings
    public bool autoUpdate;

    // create noise map and draw onto plane
    public void GenerateMap() {
        // create noise map using Perlin Noise
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

        // display map on plane
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }

    // called whenever a value is changed
    private void OnValidate() {
        // clamp values
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}
