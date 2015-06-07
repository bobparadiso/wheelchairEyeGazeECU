//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using UnityEngine;

[AddComponentMenu("Tobii EyeX/EyeX Engine Availability")]
public class EyeXEngineAvailabilityComponent : MonoBehaviour
{
    /// <summary>
    /// Gets a value indicating if the EyeX Engine is available on the system.
    /// </summary>
    public bool IsEyeXAvailable { get; private set; }

    void Awake()
    {
        IsEyeXAvailable = EyeXHost.EyeXAvailability != EyeXEngineAvailability.NotAvailable;
    }
}
