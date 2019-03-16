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
	public delegate bool GetKeyType(KeyCode code);
	public delegate bool GetMouseButtonType(int button);
	public delegate Vector3 Vector3Type();
	public delegate Vector2 Vector2Type();

	// public Fields for Management via Editor
	public bool active = false;
	public Mode mode = Mode.Record;
	public UpdateFunction UpdateCycle = UpdateFunction.Update;
	public string FilePath = "Temp/input.json";
	public bool manualStart = false;

	// public delegates for input
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

	// private types
	[Serializable]
	private struct InputSequence
	{
		public float time;
		public List<KeyCode> getKey;
		public List<KeyCode> getKeyDown;
		public List<KeyCode> getKeyUp;
		public Vector3 mousePosition;
		public Vector3 mouseWorldPosition;
		public Vector2 mouseScrollDelta;

		public void init()
		{
			getKey = new List<KeyCode> ();
			getKeyDown = new List<KeyCode> ();
			getKeyUp = new List<KeyCode> ();
			mousePosition = new Vector3 ();
			mouseWorldPosition = new Vector3 ();
			mouseScrollDelta = new Vector2 ();
		}
	};

	// private
	private StreamReader inputPlaybackStream;
	private StreamWriter inputRecordStream;

	private InputSequence oldSequence;
	private InputSequence currentSequence;
	private InputSequence nextSequence;

	private delegate void Work(float time);
	private Work work;
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

	private void SetInputStd()
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
	}

	private void SetInputFake()
	{
		GetKey = _GetKey;
		GetKeyDown = _GetKeyDown;
		GetKeyUp = _GetKeyUp;
		GetMouseButton = _GetMouseButton;
		GetMouseButtonDown = _GetMouseButtonDown;
		GetMouseButtonUp = _GetMouseButtonUp;

		_mousePosition = delegate { return currentSequence.mousePosition; };
		_mouseWorldPosition = delegate { return currentSequence.mouseWorldPosition; };
		_mouseScrollDelta = delegate { return currentSequence.mouseScrollDelta; };
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
		currentSequence.time = time;

		foreach (KeyCode vkey in System.Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKey (vkey))
				currentSequence.getKey.Add (vkey);
			if (Input.GetKeyDown (vkey))
				currentSequence.getKeyDown.Add (vkey);
			if (Input.GetKeyUp (vkey))
				currentSequence.getKeyUp.Add (vkey);
		}

		currentSequence.mousePosition = Input.mousePosition;

		currentSequence.mouseWorldPosition = Camera.main.ScreenToWorldPoint (currentSequence.mousePosition);

		currentSequence.mouseScrollDelta = Input.mouseScrollDelta;

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
		if(!Enumerable.SequenceEqual(seqA.getKey, seqB.getKey)) return true;
		else if (seqA.mousePosition != seqB.mousePosition) return true;
		else if (seqA.mouseWorldPosition != seqB.mouseWorldPosition) return true;
		else if (seqA.mouseScrollDelta != seqB.mouseScrollDelta) return true;
		else return false;
	}

	/* PLAYBACK
	 * */
	private void Play(float time)
	{
		if (time >= nextSequence.time)
		{
			oldSequence = currentSequence;
			currentSequence = nextSequence;
			Debug.Log (time);

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
	private bool _GetKey(KeyCode code) { return GetKeyCodeInList (code, currentSequence.getKey); }
	private bool _GetKeyDown(KeyCode code) { return GetKeyCodeInList (code, currentSequence.getKeyDown); }
	private bool _GetKeyUp(KeyCode code) { return GetKeyCodeInList (code, currentSequence.getKeyUp); }
	private bool _GetMouseButton(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKey); }
	private bool _GetMouseButtonDown(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKeyDown); }
	private bool _GetMouseButtonUp(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKeyUp); }
	/* in case of removed up and down lists, compare old and current sequence
	public bool GetKeyDown(KeyCode code) { return !GetKeyCodeInList (code, oldSequence.getKey) & GetKeyCodeInList (code, currentSequence.getKey); }
	public bool GetKeyUp(KeyCode code) { return GetKeyCodeInList (code, oldSequence.getKey) & !GetKeyCodeInList (code, currentSequence.getKey); }
	public bool GetMouseButtonDown(int button) { return !GetKeyCodeInList (KeyCode.Mouse0+button, oldSequence.getKey) & GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKey); }
	public bool GetMouseButtonUp(int button) { return GetKeyCodeInList (KeyCode.Mouse0+button, oldSequence.getKey) & !GetKeyCodeInList (KeyCode.Mouse0+button, currentSequence.getKey); }
	*/
}
