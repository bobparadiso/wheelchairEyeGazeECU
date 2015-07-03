using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.IO;

public class UnityArduino : MonoBehaviour
{
	const float TIME_NOT_SET = -1.0f;
	const float SEND_FREQUENCY = 0.1f;
	const float DATA_TIMEOUT = 0.5f;
	const float GAZE_POINT_EXPIRATION = 0.1f;

	public const string CONTROL_STOP = ".";

	static SerialPort serial;
	static string controlData = CONTROL_STOP;
	static float lastDataUpdate = 0.0f;

	static bool streamControlData = false;

	private GazePointDataComponent gazePointData;

	private double lastGazePoint_Timestamp = TIME_NOT_SET;
	private double lastGazePoint_Time = TIME_NOT_SET;

	public static bool validGaze = false;

	//
	static bool OpenSerialPort()
	{
		serial = new SerialPort();
		serial.BaudRate = 115200;
		serial.Parity = Parity.None;
		serial.DataBits = 8;
		serial.StopBits = StopBits.One;
		
		Debug.Log("Available Ports:");
		foreach (string s in SerialPort.GetPortNames())
		{
			Debug.Log("trying serial port: " + s);
			serial.PortName = s;
			serial.Open();
			if (serial.IsOpen)
			{
				Debug.Log ("success!");
				return true;
			}
			else
			{
				Debug.Log ("no dice");
			}
		}
		
		return false;
	}

	//
	void Awake()
	{
		DontDestroyOnLoad(transform.gameObject);
	}

	// Use this for initialization
	void Start () {
		gazePointData = GetComponent<GazePointDataComponent>();

		OpenSerialPort();
		InvokeRepeating("SendControlData", 0, SEND_FREQUENCY);

		Application.LoadLevel("drive");
		UnityArduino.SendImmediate("1");
		UnityArduino.SetStreamControlData(true);
	}

	//
	void Update()
	{
		// Get the last gaze point and timing
		var lastGazePoint =	gazePointData.LastGazePoint;
		if (lastGazePoint.IsValid && lastGazePoint.IsWithinScreenBounds &&
		    lastGazePoint.Timestamp != lastGazePoint_Timestamp)
		{
			lastGazePoint_Timestamp = lastGazePoint.Timestamp;
			lastGazePoint_Time = Time.time;
		}
		validGaze = lastGazePoint_Time != TIME_NOT_SET && Time.time - lastGazePoint_Time < GAZE_POINT_EXPIRATION;
	}

	//
	void OnDestroy() {
		SendImmediate("0");
		serial.Close();
	}

	//
	public static void SetStreamControlData(bool enable)
	{
		streamControlData = enable;
	}

	//
	public static void SetControlData(string data)
	{
		controlData = data;
		lastDataUpdate = Time.time;
	}

	//
	void SendControlData()
	{
		if (streamControlData)
		{
			if (Time.time - lastDataUpdate > DATA_TIMEOUT)
				controlData = CONTROL_STOP;

			serial.Write(controlData);
			serial.BaseStream.Flush();
		}
	}

	//
	public static void SendImmediate(string data)
	{
		serial.Write(data);
		serial.BaseStream.Flush();
	}
}
