using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class Noise {

    // take in values for noise variables and create a 2d array of heights using Perlin Noise
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // pseudo random number generator that allows seeds to generate the same map every time
        System.Random prng = new System.Random(seed);

        // offsets to sample each octave from different location
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++) {
            // generate offsets based on seed, avoid using numbers too large for prng by using range (-100000, 100000)
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;

            // store x and y offsets in the array
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // protect against a divide by zero
        if (scale <= 0) scale = 0.0001f;

        // values to keep track of maximum and minimum noise heights
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // calculate values for center of the map
        float halfWidth = mapWidth / 2.0f;
        float halfHeight = mapHeight / 2.0f;

        // loop through noise map and generate height values
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                // create amplitude,frequency, and keep track of current height value
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                // loop through all octaves
                for (int i = 0; i < octaves; i++) {
                    // get height sample point, subtract center of map to make changing scale zoom in and out of center
                    float sampleX = (x - halfHeight) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    // get Perlin value in the range [-1, 1]
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // increase noise height by perlin value of each octave
                    noiseHeight += perlinValue * amplitude;

                    // change amplitdue and frequency based on persistence and lacunarity at the end of each octave
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // keep track of maximum and minimum noise heights
                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }

                // apply noise height to noise map
                noiseMap[x, y] = noiseHeight;
            }
        }

        // loop through noise map
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                // normalize points by returning value in range [0, 1] based on minimum and maximum heights generated
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

}
