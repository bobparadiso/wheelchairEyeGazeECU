//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using Tobii.EyeX.Framework;

public partial class EyeXHost
{
    /// <summary>
    /// Gets the availability of the EyeX Engine.
    /// </summary>
    public static EyeXEngineAvailability EyeXAvailability
    {
        get { return (EyeXEngineAvailability)Tobii.EyeX.Client.Environment.GetEyeXAvailability(); }
    }

    /// <summary>
    /// Starts the recalibration tool.
    /// </summary>
    public void LaunchRecalibration()
    {
        _context.LaunchConfigurationTool(ConfigurationTool.Recalibrate, data => { });
    }

    /// <summary>
    /// Starts the calibration testing tool.
    /// </summary>
    public void LaunchCalibrationTesting()
    {
        _context.LaunchConfigurationTool(ConfigurationTool.TestEyeTracking, data => { });
    }
}
