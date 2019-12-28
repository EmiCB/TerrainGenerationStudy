using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatch to game object
public class MapGenerator : MonoBehaviour {
    // map dimensions
    public int mapWidth;
    public int mapHeight;

    // noise properties
    public float noiseScale;

    // create noise map and draw onto plane
    public void GenerateMap() {
        // create noise map using Perlin Noise
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale);

        // display map on plane
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}
