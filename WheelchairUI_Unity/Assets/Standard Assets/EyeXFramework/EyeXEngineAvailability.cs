//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

/// <summary>
/// Describes the availability statuses of the EyeX Engine.
/// </summary>
public enum EyeXEngineAvailability
{
    /// <summary>
    /// The EyeX Engine is not available.
    /// </summary>
    NotAvailable = 1,

    /// <summary>
    /// The EyeX Engine is installed but not running.
    /// </summary>
    NotRunning = 2,

    /// <summary>
    /// The EyeX Engine is running.
    /// </summary>
    Running = 3,
}
