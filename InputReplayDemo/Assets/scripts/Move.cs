using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour {

    InputReplay myInput;
    Vector3 newposition;

    // Use this for initialization
    void Start () {
        myInput = this.gameObject.GetComponent<InputReplay>();
    }
	
	// Update is called once per frame
	void Update () {
        newposition = this.transform.position;
        if (myInput.GetKey(KeyCode.LeftArrow))
            newposition.x -= 0.01f;
        if (myInput.GetKey(KeyCode.RightArrow))
            newposition.x += 0.01f;
        if (myInput.GetKey(KeyCode.UpArrow))
            newposition.y += 0.01f;
        if (myInput.GetKey(KeyCode.DownArrow))
            newposition.y -= 0.01f;

        this.transform.position = newposition;

    }
}
