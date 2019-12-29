using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatch to game object
public class MapGenerator : MonoBehaviour {
    // easily swap what type of map to display
    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;

    // map dimensions
    public int mapWidth;
    public int mapHeight;

    // noise properties
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]                //make persistence value into a slider
    public float persistence;
    public float lacunarity;

    // allows for scrolling around the map
    public Vector2 offset;

    // editor settings
    public bool autoUpdate;

    // list of terrain types
    public TerrainTypes[] regions;

    // create noise map and draw onto plane
    public void GenerateMap() {
        // create noise map using Perlin Noise
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

        // array of all pixel colors, region colors will be stored to it
        Color[] colorMap = new Color[mapWidth * mapHeight];

        // loop through noise map to assign regions
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                // get current height of point
                float currentHeight = noiseMap[x, y];

                // loop through regions and assign them to points
                for (int i = 0; i < regions.Length; i++) {
                    // check if height value is within region's height range
                    if (currentHeight <= regions[i].height) {
                        //save new region color to point and exit loop (no need to check other regions)
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        // display map on plane
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // check draw mode and display map accordingly
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }
        else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }
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

// properties of terrain types based on height of map, shows in inspector
[System.Serializable]
public struct TerrainTypes {
    public string name;
    public float height;
    public Color color;
}