using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyScript : MonoBehaviour {

    InputReplay myInput;

	// Use this for initialization
	void Start () {
        myInput = this.gameObject.GetComponent<InputReplay>();
	}
	
	// Update is called once per frame
	void Update () {
        if (myInput.GetKey(KeyCode.A))
            Debug.Log("A");
	}
}
