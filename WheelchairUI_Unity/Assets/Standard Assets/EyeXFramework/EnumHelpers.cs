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

            default:
                throw new InvalidOperationException("Unknown user presence value.");
        }
    }
}
