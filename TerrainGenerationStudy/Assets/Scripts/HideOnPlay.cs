using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnPlay : MonoBehaviour {
	// runs once at the beginning of play mode
	void Start() {
        gameObject.SetActive(false);
    }
}
