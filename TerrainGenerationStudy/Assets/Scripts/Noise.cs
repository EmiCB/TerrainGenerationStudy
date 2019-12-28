using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class Noise {

    // take in values for noise variables and create a 2d array of heights using Perlin Noise
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // protect against a divide by zero
        if (scale <= 0) scale = 0.0001f;

        // loop through noise map and generate height values
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                // get height sample point
                float sampleX = x / scale;
                float sampleY = y / scale;

                // get Perlin value and apply it to noise map
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }

}
