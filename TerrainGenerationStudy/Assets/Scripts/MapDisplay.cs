using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attaches to gameobject, turns noise map into a texture which is applied to a plane
public class MapDisplay : MonoBehaviour {

    // renderer of plane (to set texture)
    public Renderer textureRenderer;

    // draws noise map onto plane
    public void DrawTexture(Texture2D texture) {
        // set texture on plane and make it the same size as the map
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}
