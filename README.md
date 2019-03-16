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
![parameter](https://previews.dropbox.com/p/thumb/AAXneT5oTptfoSqf3bCAx9XOQ0peVOffRaEFR0y-Vb__ppIsnKNS7IZeee-8q8KFa4YvdbAANlNVcCUrKnLgLfAxzQiPuSrwuoDWkAIc5512N8LvIZ6JsJXCF3fdqHhg526icgfdKsZU65qfCxCmPUjB-gM8vVR8EkO38ZT08yxwnj_lbjmXzH46UMx3iFB2KbMscWSDf6jDLccYzzWFAgon_N7tTWurSpyv31Ap4-lnoGRBdTcErrtDICrmUhaICguT5Y0x_tEvFBv4Y6REvJYk5ocFxAuEem5LDH_85zWcpA/p.png?size_mode=5)
##### Record mode
* activate it, select the mode (record), Update Cycle (Update or FixedUpdate) and file 
* start the game and smash your keyboard !
##### Player mode
* set the file path and player mode
* read the input from the InputReplay class with the following methods and var :
```csharp
public bool GetKey(KeyCode code)
public bool GetKey(KeyCode code)
public bool GetKeyDown(KeyCode code)
public bool GetKeyUp(KeyCode code)
public bool GetMouseButton(int button)
public bool GetMouseButtonDown(int button)
public bool GetMouseButtonUp(int button)

public Vector3 mousePosition { get; }
public Vector3 mouseWorldPosition { get; }
public Vector2 mouseScrollDelta { get; }
```



