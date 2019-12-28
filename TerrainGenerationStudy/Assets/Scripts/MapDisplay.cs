using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attaches to gameobject, turns noise map into a texture which is applied to a plane
public class MapDisplay : MonoBehaviour {

    // render of plane (to set texture)
    public Renderer textureRenderer;

    // draws noise map onto plane
    public void DrawNoiseMap(float[,] noiseMap) {
        // find noise map dimensions
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        // create texture
        Texture2D texture = new Texture2D(width, height);

        // generate array of all possible pixel colors
        Color[] colorMap = new Color[width * height];
        // loop through noise map and set all pixel colors based on Perlin value
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        // apply colors to texture
        texture.SetPixels(colorMap);
        texture.Apply();

        // set texture on plane and make it the same size as the map
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }
}
