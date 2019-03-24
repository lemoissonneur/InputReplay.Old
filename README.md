# InputReplay

InputReplay is a basic keyboard and mouse recorder and player for unity.
***



# fonctions
* You can manualy start and stop the record/playback in your code with `StartRecord()`, `StartPlayBack()` and `Stop()`.
* Will return value from `UnityEngine.Input` if not activated or in record mode, so you don't need to change your code everytime, just desactivate it.
* Use json, so you can easily create input sequence to automate tests in playMode
* Playback is time based.
* Cannot record and play at the same time.
* <5 ms delay in `Update` cycle.
***



# How to use
* add the InputReplay.cs script to and empty GameObject
![parameter](https://github.com/lemoissonneur/InputReplay/blob/master/doc/images/InputReplay_param_github.PNG)
##### Record mode
* activate it, select the mode (record), Update Cycle (Update or FixedUpdate) and file 
* start the game and smash your keyboard !
##### Player mode
* set the file path and player mode
* read the input from the 'InputReplay' just like you would read from 'UnityEngine.Input' with the supported methods and properties :
```csharp
public bool GetKey(KeyCode code);
public bool GetKeyDown(KeyCode code);
public bool GetKeyUp(KeyCode code);
public bool GetMouseButton(int button);
public bool GetMouseButtonDown(int button);
public bool GetMouseButtonUp(int button);
public bool GetButton(string name);
public bool GetButtonDown(string name);
public bool GetButtonUp(string name);
public float GetAxis(string name);

public bool anyKey { get; }
public bool anyKeyDown { get; }
public Vector3 mousePosition { get; }
public Vector3 mouseWorldPosition { get; }
public Vector2 mouseScrollDelta { get; }
```

# Example :
this code will work in both record and replay mode
```csharp
public class Test1 : MonoBehaviour {

	InputReplay myInput;
  
	// Use this for initialization
	void Start () {
		myInput = GameObject.Find ("InputReplay").GetComponent<InputReplay> ();
	}
	
	// Update is called once per frame
	void Update () {
		if(myInput.GetKey (KeyCode.A))
			Debug.Log('A');
		
	}
}
```

