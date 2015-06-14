using UnityEngine;

public class gazeButton : MonoBehaviour 
{
	const float ACTIVATE_THRESHOLD = 3.0f;
	const float TIME_NOT_SET = -1.0f;
	const float COOL_DOWN_DURATION = 0.5f;

	private Transform _bgTransform;
	private SpriteRenderer _bgSpriteRenderer;
	private BoxCollider2D _bgCollider2D;
	private GazeAwareComponent _bgGazeAware;

	private float lastGazeTime = TIME_NOT_SET;
	private float lastActivateTime = TIME_NOT_SET;

	public string targetScene = "";
	public string controlData = "";
	
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

		if (lastActivateTime != TIME_NOT_SET)
		{
			if (Time.time - lastActivateTime < COOL_DOWN_DURATION)
			{
				hasGaze = false;
			}
			else
			{
				lastActivateTime = TIME_NOT_SET;
			}
		}

		//update gaze time
		if (hasGaze)
		{
			_bgTransform.Rotate (Vector3.forward);
			if (lastGazeTime == TIME_NOT_SET)
				lastGazeTime = Time.time;
		}
		else if (lastGazeTime != TIME_NOT_SET)
		{
			lastGazeTime = TIME_NOT_SET;
		}

		//respond to gaze time
		if (lastGazeTime == TIME_NOT_SET)
		{
			if (lastActivateTime == TIME_NOT_SET)
				_bgSpriteRenderer.color = Color.red;
			else
				_bgSpriteRenderer.color = Color.green;
		}
		else if (Time.time - lastGazeTime < ACTIVATE_THRESHOLD)
			_bgSpriteRenderer.color = Color.yellow;
		else
		{
			_bgSpriteRenderer.color = Color.green;
			if (targetScene != "")
			{
				Application.LoadLevel(targetScene);
				if (targetScene == "drive" || targetScene == "arm")
					UnityArduino.SetStreamControlData(true);
				else
					UnityArduino.SetStreamControlData(false);
			}
			if (controlData != "")
				UnityArduino.SendImmediate(controlData);

			lastActivateTime = Time.time;
		}
	}
}
