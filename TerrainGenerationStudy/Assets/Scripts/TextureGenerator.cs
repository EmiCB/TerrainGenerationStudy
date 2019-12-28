using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will not be attached to gameobject nor have multiple instances
public static class TextureGenerator {
    // creates texture out of one-dimensional color map
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height) {
        // create texture
        Texture2D texture = new Texture2D(width, height);

        // apply colors to texture, make texture non-repeting and less blurry
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
		texture.Apply();

		return texture;
	}

	// creates texture out of two-dimensional height map
    public static Texture2D TextureFromHeightMap(float[,] heightMap) {
        // find noise map dimensions
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // generate array of all possible pixel colors
        Color[] colorMap = new Color[width * height];
        // loop through noise map and set all pixel colors based on Perlin value
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }
}
