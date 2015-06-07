using UnityEngine;

public class gazeSwitch : MonoBehaviour 
{
	const float ACTIVATE_THRESHOLD = 3.0f;
	const float TIME_NOT_SET = -1.0f;

	private GazeAwareComponent _gazeAware;
	private SpriteRenderer _spriteRenderer;

	private float lastGazeTime = TIME_NOT_SET;

	public string controlData = ".";
	
	void Start () 
	{
		_gazeAware = GetComponent<GazeAwareComponent>();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}
	
	void Update () 
	{
		//update gaze time
		if (_gazeAware.HasGaze)
		{
			transform.Rotate (Vector3.forward);
			if (lastGazeTime == TIME_NOT_SET)
				lastGazeTime = Time.time;
		}
		else if (lastGazeTime != TIME_NOT_SET)
		{
			UnityArduino.SetControlData(UnityArduino.CONTROL_STOP);
			lastGazeTime = TIME_NOT_SET;
		}

		//respond to gaze time
		if (lastGazeTime == TIME_NOT_SET)
			_spriteRenderer.color = Color.red;
		else if (Time.time - lastGazeTime < ACTIVATE_THRESHOLD)
			_spriteRenderer.color = Color.yellow;
		else
		{
			_spriteRenderer.color = Color.green;
			UnityArduino.SetControlData(controlData);
		}
	}
}
