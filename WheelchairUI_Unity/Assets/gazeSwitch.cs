using UnityEngine;

public class gazeSwitch : MonoBehaviour 
{
	const float ACTIVATE_THRESHOLD = 1.5f;
	const float DEACTIVATE_THRESHOLD = 0.25f;
	const float TIME_NOT_SET = -1.0f;

	private Transform _bgTransform;
	private SpriteRenderer _bgSpriteRenderer;
	private BoxCollider2D _bgCollider2D;
	private GazeAwareComponent _bgGazeAware;

	private float lastGazeTime = TIME_NOT_SET;
	private float lastBreakTime = TIME_NOT_SET;

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
		bool hasGaze = _bgGazeAware.HasGaze;

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
				if (lastBreakTime == TIME_NOT_SET)
					lastBreakTime = Time.time;
				else if (Time.time - lastBreakTime >= DEACTIVATE_THRESHOLD)
				{
					UnityArduino.SetControlData(UnityArduino.CONTROL_STOP);
					lastGazeTime = TIME_NOT_SET;
				}
			}
			else
			{
				UnityArduino.SetControlData(UnityArduino.CONTROL_STOP);
				lastGazeTime = TIME_NOT_SET;
			}
		}

		//respond to gaze time
		if (lastGazeTime == TIME_NOT_SET)
			_bgSpriteRenderer.color = Color.red;
		else if (Time.time - lastGazeTime < ACTIVATE_THRESHOLD)
			_bgSpriteRenderer.color = Color.yellow;
		else
		{
			_bgSpriteRenderer.color = Color.green;
			UnityArduino.SetControlData(controlData);
		}
	}
}
