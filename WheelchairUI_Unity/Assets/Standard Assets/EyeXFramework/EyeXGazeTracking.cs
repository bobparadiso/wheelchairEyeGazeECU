//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

/// <summary>
/// Represents different gaze tracking states.
/// </summary>
public enum EyeXGazeTracking
{
	/// <summary>
	/// User presence is unknown.
	/// This might be due to an error such as the eye tracker not tracking.
	/// </summary>
	Unknown = 0,	
	/// <summary>
	/// The user is present.
	/// </summary>
	GazeTracked = 1,
	/// <summary>
	/// The user is not present.
	/// </summary>
	GazeNotTracked = 2,
	/// <summary>
	/// Gaze tracking state is not supported
	/// in the version of EyeX Engine that the user has.
	/// </summary>
	NotSupported = 3,
}
