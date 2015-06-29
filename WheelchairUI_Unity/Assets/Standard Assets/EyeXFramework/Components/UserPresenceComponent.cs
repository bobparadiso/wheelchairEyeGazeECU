//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Component that encapsulates the <see cref="EyeXHost.UserPresence"/> state.
/// </summary>
[AddComponentMenu("Tobii EyeX/User Presence")]
public class UserPresenceComponent : MonoBehaviour
{
    private EyeXHost _eyexHost;

    /// <summary>
    /// Gets a value indicating whether the user presence state is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is present in front of the screen.
    /// </summary>
    public bool IsUserPresent { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the EyeX Engine is tracking the user's gaze.
	/// </summary>
	public EyeXGazeTracking GazeTracking { get; private set; }

    void Start()
    {
        _eyexHost = EyeXHost.GetInstance();
    }

    void Update()
    {
        var userPresenceStateValue = _eyexHost.UserPresence;

        IsValid = userPresenceStateValue != EyeXUserPresence.Unknown;
        IsUserPresent = (userPresenceStateValue == EyeXUserPresence.Present);
		GazeTracking = _eyexHost.GazeTracking;
    }
}
