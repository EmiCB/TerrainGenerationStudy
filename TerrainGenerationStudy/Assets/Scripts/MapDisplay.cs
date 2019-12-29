using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attaches to gameobject, turns noise map into a texture which is applied to a plane
public class MapDisplay : MonoBehaviour {

    // renderer of plane (to set texture)
    public Renderer textureRenderer;

    // filter and renderer of mesh (to create mesh and set texture)
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    // draws noise map onto plane
    public void DrawTexture(Texture2D texture) {
        // set texture on plane and make it the same size as the map
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    // draws mesh from noise map
    public void DrawMesh(MeshData meshData, Texture2D texture) {
        // shared allows mesh to be generated outside of play mode
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
