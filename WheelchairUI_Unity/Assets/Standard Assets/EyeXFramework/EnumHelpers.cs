//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using Tobii.EyeX.Framework;

internal static class EnumHelpers
{
    public static EyeXDeviceStatus ConvertToEyeXDeviceStatus(EyeXEngineStateValue<EyeTrackingDeviceStatus> state)
    {
        if (state == null || !state.IsValid)
        {
            return EyeXDeviceStatus.Unknown;
        }

        switch (state.Value)
        {
            // Pending?
            case EyeTrackingDeviceStatus.Initializing:
            case EyeTrackingDeviceStatus.Configuring:
                return EyeXDeviceStatus.Pending;

            // Tracking?
            case EyeTrackingDeviceStatus.Tracking:
                return EyeXDeviceStatus.Tracking;

            // Disabled?
            case EyeTrackingDeviceStatus.TrackingPaused:
                return EyeXDeviceStatus.Disabled;

            // Not available
            default:
                return EyeXDeviceStatus.NotAvailable;
        }
    }

    public static EyeXUserPresence ConvertToEyeXUserPresence(EyeXEngineStateValue<UserPresence> state)
    {
        if (state == null || !state.IsValid)
        {
            return EyeXUserPresence.Unknown;
        }

        switch (state.Value)
        {
            // Present?
            case UserPresence.Present:
                return EyeXUserPresence.Present;

            // Not present?
            case UserPresence.NotPresent:
                return EyeXUserPresence.NotPresent;

            // Unknown?
            case UserPresence.Unknown:
                return EyeXUserPresence.Unknown;

            default:
                throw new InvalidOperationException("Unrecognized user presence value.");
        }
    }

    public static EyeXGazeTracking ConvertToEyeXGazeTracking(EyeXHost host, EyeXEngineStateValue<GazeTracking> state)
    {
        if(host.EngineVersion == null || (host.EngineVersion != null && host.EngineVersion.Major >= 1 && host.EngineVersion.Minor >= 4))
        {
            if (state == null || !state.IsValid || state.Value == 0)
            {
                return EyeXGazeTracking.Unknown;
            }

            switch (state.Value)
            {
                // Gaze tracked?
                case GazeTracking.GazeTracked:
                    return EyeXGazeTracking.GazeTracked;

                // Gaze not tracked?
                case GazeTracking.GazeNotTracked:
                    return EyeXGazeTracking.GazeNotTracked;

                default:
                    throw new InvalidOperationException("Unknown gaze tracking value.");
            }
        }
        return EyeXGazeTracking.NotSupported;
    }
}
