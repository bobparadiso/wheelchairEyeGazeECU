using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.IO;

public class UnityArduino : MonoBehaviour
{
	const float SEND_FREQUENCY = 0.1f;
	const float DATA_TIMEOUT = 0.5f;
	public const string CONTROL_STOP = ".";

	static SerialPort serial;
	static string controlData = CONTROL_STOP;
	static float lastDataUpdate = 0.0f;

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
	
	// Use this for initialization
	void Start () {
		OpenSerialPort();
		InvokeRepeating("SendControlData", 0, SEND_FREQUENCY);
	}

	public static void SetControlData(string data)
	{
		controlData = data;
		lastDataUpdate = Time.time;
	}

	//
	void SendControlData()
	{
		if (Time.time - lastDataUpdate > DATA_TIMEOUT)
			controlData = CONTROL_STOP;

		serial.Write(controlData);
	}

}
