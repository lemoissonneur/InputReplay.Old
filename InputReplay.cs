using System;
using System.IO;	// for stream
using System.Linq;	// for List compare
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputReplay : MonoBehaviour {

	// public types
	public enum Mode {Record, PlayBack};
	public enum UpdateFunction {FixedUpdate, Update, Both};
	// public types for delegates
	public delegate bool AnyKeyType();
	public delegate bool GetKeyType(KeyCode code);
	public delegate bool GetMouseButtonType(int button);
	public delegate Vector3 Vector3Type();
	public delegate Vector2 Vector2Type();

	// public properties for Management via Editor UI
	public bool active = false;
	public Mode mode = Mode.Record;
	public UpdateFunction UpdateCycle = UpdateFunction.Update;
	public string FilePath = "Temp/input.json";
	public bool manualStart = false;

	// public methods and properties for input access
	public GetKeyType GetKey;
	public GetKeyType GetKeyDown;
	public GetKeyType GetKeyUp;
	public GetMouseButtonType GetMouseButton;
	public GetMouseButtonType GetMouseButtonDown;
	public GetMouseButtonType GetMouseButtonUp;

	private Vector3Type _mousePosition;
	private Vector3Type _mouseWorldPosition;
	private Vector2Type _mouseScrollDelta;
	public Vector3 mousePosition { get { return _mousePosition(); } }
	public Vector3 mouseWorldPosition { get { return _mouseWorldPosition(); } }
	public Vector2 mouseScrollDelta { get { return _mouseScrollDelta(); } }

	private AnyKeyType _anyKey;
	private AnyKeyType _anyKeyDown;
	public bool anyKey { get { return _anyKey (); } }
	public bool anyKeyDown { get { return _anyKeyDown (); } }

	// private types
	[Serializable]
	private struct InputSequence		// var names are reduced for smaller json
	{
		public float t;			// time
		public List<KeyCode> gK;	// getKey
		public List<KeyCode> gKD;	// getKeyDown
		public List<KeyCode> gKU;	// getKeyUp
		public Vector3 mP;		// mousePosition
		public Vector3 mWP;		// mouseWorldPosition
		public Vector2 mSD;		// mouseScrollDelta

		public void init()
		{
			gK = new List<KeyCode> ();
			gKD = new List<KeyCode> ();
			gKU = new List<KeyCode> ();
			mP = new Vector3 ();
			mWP = new Vector3 ();
			mSD = new Vector2 ();
		}
	};

	// Streams
	private StreamReader inputPlaybackStream;
	private StreamWriter inputRecordStream;

	// Input sequences
	private InputSequence oldSequence;
	private InputSequence currentSequence;
	private InputSequence nextSequence;

	// delegate to switch from record to play activity
	private delegate void Work(float time);
	private Work work;

	// start time of the record or playback
	private float startTime = 0.0f;



	// Use this for initialization
	void Start ()
	{
		if (!active)	// if not active, switch to Input
		{
			SetInputStd ();
			work = pause;
			return;
		}

		switch (mode)
		{
		case Mode.Record:
			oldSequence.init ();
			currentSequence.init ();

			inputRecordStream = new StreamWriter (FilePath, false);	// will overwrite new file Stream
			if (inputRecordStream.ToString () == "")
			{
				Stop ();
				Debug.Log ("InputReplay: StreamWriter(" + FilePath + "), file not found ?");
			}
			else
			{
				inputRecordStream.AutoFlush = true;
				work = Record;
				SetInputStd ();
			}
			break;

		case Mode.PlayBack:
			oldSequence.init ();
			currentSequence.init ();
			nextSequence.init ();

			inputPlaybackStream = new StreamReader (FilePath, false);
			if (inputPlaybackStream.ToString () == "")
			{
				Stop ();
				Debug.Log ("InputReplay: StreamReader(" + FilePath + "), file not found ?");
			}
			else if (!ReadLine ()) // read the first line to check
			{
				Stop ();
				Debug.Log ("InputReplay: empty file");
			}
			else
			{
				work = Play;
				SetInputFake ();
			}
			break;
		}

		if (manualStart)
		{
			SetInputStd ();
			work = pause;
		}
	}

	~InputReplay()
	{
		Stop ();
	}

	// Update is called once per frame
	void Update ()
	{
		if (UpdateCycle == UpdateFunction.Update || UpdateCycle == UpdateFunction.Both)
			work(Time.time - startTime);
	}

	// FixedUpdate is called every physics update
	void FixedUpdate ()
	{
		if (UpdateCycle == UpdateFunction.FixedUpdate || UpdateCycle == UpdateFunction.Both)
			work(Time.fixedTime - startTime);
	}

	/*
	 * PUBLIC METHODS
	 * */
	public void Stop()
	{
		active = false;

		switch (mode) {
		case Mode.Record:
			inputRecordStream.Close ();
			break;
		case Mode.PlayBack:
			inputPlaybackStream.Close ();
			break;
		}

		work = pause;
	}

	public void StartRecord()
	{
		setStartTime ();
		work = Record;
	}

	public void StartPlayBack()
	{
		setStartTime ();
		SetInputFake ();
		work = Play;
	}

	/*
	 * PRIVATE METHODS
	 * */
	private void setStartTime()
	{
		if (UpdateCycle == UpdateFunction.FixedUpdate)
			startTime = Time.fixedTime;
		else
			startTime = Time.time;
	}

	private void SetInputStd()	// redirect public methods and properties to actual UnityEngine.Input
	{
		GetKey = Input.GetKey;
		GetKeyDown = Input.GetKeyDown;
		GetKeyUp = Input.GetKeyUp;
		GetMouseButton = Input.GetMouseButton;
		GetMouseButtonDown = Input.GetMouseButtonDown;
		GetMouseButtonUp = Input.GetMouseButtonUp;

		_mousePosition = delegate { return Input.mousePosition; };
		_mouseWorldPosition = delegate { return Camera.main.ScreenToWorldPoint (Input.mousePosition); };
		_mouseScrollDelta = delegate { return Input.mouseScrollDelta; };

		_anyKey = delegate { return Input.anyKey; };
		_anyKeyDown = delegate { return Input.anyKeyDown; };
	}

	private void SetInputFake()	// redirect public methods and properties to our replay system
	{
		GetKey = FakeGetKey;
		GetKeyDown = FakeGetKeyDown;
		GetKeyUp = FakeGetKeyUp;
		GetMouseButton = FakeGetMouseButton;
		GetMouseButtonDown = FakeGetMouseButtonDown;
		GetMouseButtonUp = FakeGetMouseButtonUp;

		_mousePosition = delegate { return currentSequence.mP; };
		_mouseWorldPosition = delegate { return currentSequence.mWP; };
		_mouseScrollDelta = delegate { return currentSequence.mSD; };

		_anyKey = delegate { return currentSequence.gK.Any<KeyCode>(); };
		_anyKeyDown = delegate { return currentSequence.gKD.Any<KeyCode>(); };
	}

	private void pause(float time)
	{
		// nothing to do
	}

	/* RECORD
	 * */
	private void Record(float time)
	{
		currentSequence.init ();
		currentSequence.t = time;

		foreach (KeyCode vkey in System.Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKey (vkey))
				currentSequence.gK.Add (vkey);
			if (Input.GetKeyDown (vkey))
				currentSequence.gKD.Add (vkey);
			if (Input.GetKeyUp (vkey))
				currentSequence.gKU.Add (vkey);
		}

		currentSequence.mP = Input.mousePosition;

		currentSequence.mWP = Camera.main.ScreenToWorldPoint (currentSequence.mP);

		currentSequence.mSD = Input.mouseScrollDelta;

		// if nothing new, we dont write anything
		if (AnyChange(oldSequence, currentSequence))
		{
			//Debug.Log (JsonUtility.ToJson (newSequence));
			inputRecordStream.WriteLine (JsonUtility.ToJson (currentSequence));
			oldSequence = currentSequence;
		}
	}

	private bool AnyChange(InputSequence seqA, InputSequence seqB)
	{
		if(!Enumerable.SequenceEqual(seqA.gK, seqB.gK)) return true;
		else if (seqA.mP != seqB.mP) return true;
		else if (seqA.mWP != seqB.mWP) return true;
		else if (seqA.mSD != seqB.mSD) return true;
		else return false;
	}

	/* PLAYBACK
	 * */
	private void Play(float time)
	{
		if (time >= nextSequence.t)
		{
			oldSequence = currentSequence;
			currentSequence = nextSequence;
			//Debug.Log (time);

			nextSequence.init ();
			if (!ReadLine ())
			{
				Stop ();
				Debug.Log ("InputPlayback: EndOfFile");
			}
		}
	}

	private bool ReadLine()
	{
		string newline = inputPlaybackStream.ReadLine ();

		if (newline == null)
			return false;

		nextSequence = JsonUtility.FromJson<InputSequence> (newline);
		return true;
	}

	private bool GetKeyCodeInList (KeyCode code, List<KeyCode> list)
	{
		foreach (KeyCode vkey in list)
		{
			if (vkey == code)
				return true;
		}
		return false;
	}

	/*
	 * PRIVATE FAKE INPUT
	 * */
	private bool FakeGetKey(KeyCode code) { return GetKeyCodeInList (code, currentSequence.gK); }
	private bool FakeGetKeyDown(KeyCode code) { return GetKeyCodeInList (code, currentSequence.gKD); }
	private bool FakeGetKeyUp(KeyCode code) { return GetKeyCodeInList (code, currentSequence.gKU); }
	private bool FakeGetMouseButton(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.gK); }
	private bool FakeGetMouseButtonDown(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.gKD); }
	private bool FakeGetMouseButtonUp(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.gKU); }
	/* in case of removed up and down lists, compare old and current sequence
	public bool FakeGetKeyDown(KeyCode code) { return !GetKeyCodeInList (code, oldSequence.getKey) & GetKeyCodeInList (code, currentSequence.getKey); }
	public bool FakeGetKeyUp(KeyCode code) { return GetKeyCodeInList (code, oldSequence.getKey) & !GetKeyCodeInList (code, currentSequence.getKey); }
	public bool FakeGetMouseButtonDown(int button) { return !GetKeyCodeInList (KeyCode.Mouse0+button, oldSequence.getKey) & GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKey); }
	public bool FakeGetMouseButtonUp(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, oldSequence.getKey) & !GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKey); }
	*/
}
