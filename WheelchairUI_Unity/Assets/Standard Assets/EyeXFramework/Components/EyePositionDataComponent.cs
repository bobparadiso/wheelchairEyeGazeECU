//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Component that encapsulates a provider for <see cref="EyeXEyePosition"/> data.
/// </summary>
[AddComponentMenu("Tobii EyeX/Eye Position Data")]
public class EyePositionDataComponent : MonoBehaviour
{
    public FixationDataMode fixationDataMode;

    private EyeXHost _eyexHost;
    private IEyeXDataProvider<EyeXEyePosition> _dataProvider;

    /// <summary>
    /// Gets the last eye position.
    /// </summary>
    public EyeXEyePosition LastEyePosition { get; private set; }

    protected void Awake()
    {
        _eyexHost = EyeXHost.GetInstance();
        _dataProvider = _eyexHost.GetEyePositionDataProvider();
    }

    protected void OnEnable()
    {
        _dataProvider.Start();
    }

    protected void OnDisable()
    {
        _dataProvider.Stop();
    }

    protected void Update()
    {
        LastEyePosition = _dataProvider.Last;
    }
}
