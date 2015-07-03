using UnityEngine;

public class gazeSwitch : MonoBehaviour 
{
	const float ACTIVATE_THRESHOLD = 1.25f;
	const float DEACTIVATE_THRESHOLD = 0.25f;
	const float TIME_NOT_SET = -1.0f;

	private static bool activatedSwitchExists = false;

	private Transform _bgTransform;
	private SpriteRenderer _bgSpriteRenderer;
	private BoxCollider2D _bgCollider2D;
	private GazeAwareComponent _bgGazeAware;

	private float lastGazeTime = TIME_NOT_SET;
	private float lastBreakTime = TIME_NOT_SET;

	private bool activated = false;

	public string controlData = ".";

	void Start ()
	{
		_bgTransform = transform.Find("background");
		_bgSpriteRenderer = _bgTransform.GetComponent<SpriteRenderer>();
		_bgCollider2D = _bgTransform.GetComponent<BoxCollider2D>();
		_bgGazeAware = _bgTransform.GetComponent<GazeAwareComponent>();
	}
	
	void Update () 
	{
		bool hasGaze = _bgGazeAware.HasGaze && !activatedSwitchExists;

		//for now, just for debug
		if (Input.GetMouseButton(0))
		{
			Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (_bgCollider2D.OverlapPoint(worldPos))
				hasGaze = true;
		}

		//update gaze time
		if (hasGaze)
		{
			_bgTransform.Rotate (Vector3.forward);
			if (lastGazeTime == TIME_NOT_SET)
				lastGazeTime = Time.time;
			lastBreakTime = TIME_NOT_SET;
		}
		else if (lastGazeTime != TIME_NOT_SET)
		{
			if (Time.time - lastGazeTime >= ACTIVATE_THRESHOLD)
			{
				//since selection is already locked, only lack of valid gaze can deactivate
				if (!UnityArduino.validGaze)
				{
					if (lastBreakTime == TIME_NOT_SET)
						lastBreakTime = Time.time;
					else if (Time.time - lastBreakTime >= DEACTIVATE_THRESHOLD)
					{
						UnityArduino.SetControlData(UnityArduino.CONTROL_STOP);
						lastGazeTime = TIME_NOT_SET;
					}
				}
				else
					lastBreakTime = TIME_NOT_SET;
			}
			else
			{
				UnityArduino.SetControlData(UnityArduino.CONTROL_STOP);
				lastGazeTime = TIME_NOT_SET;
			}
		}

		bool prevActivated = activated;

		//respond to gaze time
		if (lastGazeTime == TIME_NOT_SET)
		{
			_bgSpriteRenderer.color = Color.red;
			activated = false;
		}
		else if (Time.time - lastGazeTime < ACTIVATE_THRESHOLD)
		{
			_bgSpriteRenderer.color = Color.yellow;
			activated = false;
		}
		else
		{
			_bgSpriteRenderer.color = Color.green;
			activated = true;
		}

		if (!prevActivated && activated)
			activatedSwitchExists = true;

		if (prevActivated && !activated)
			activatedSwitchExists = false;

		if (activated)
			UnityArduino.SetControlData(controlData);
	}
}
