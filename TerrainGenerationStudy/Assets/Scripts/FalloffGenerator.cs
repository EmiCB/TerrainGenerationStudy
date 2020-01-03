using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class FalloffGenerator {
    // create falloff map
    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size, size];

        // loop through map
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                // put coordinates in range of [-1, 1]
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // find value for map by finding x or y closest to edge of square
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    // curve for falloff severity
    static float Evaluate(float value) {
        // exponent and multiplier variables
        float a = 3.0f;
        float b = 2.2f;

        // equation of curve
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
