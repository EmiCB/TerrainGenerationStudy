using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// makes a button in the editor to generate noise maps
[CustomEditor (typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        // find map generator
        MapGenerator mapGen = (MapGenerator)target;

        // check if any value is changed and generate map if auto update is enabled
        if (DrawDefaultInspector()) {
            if (mapGen.autoUpdate) {
                mapGen.GenerateMap();
            }
        }

        // button
        if (GUILayout.Button("Generate")) {
            mapGen.GenerateMap();
        }
    }

}
