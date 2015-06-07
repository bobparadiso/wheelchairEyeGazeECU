//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Component that encapsulates a provider for <see cref="EyeXFixationPoint"/> data.
/// </summary>
[AddComponentMenu("Tobii EyeX/Fixation Data")]
public class FixationDataComponent : MonoBehaviour
{
    public FixationDataMode fixationDataMode = FixationDataMode.Sensitive;

    private EyeXHost _eyexHost;
    private IEyeXDataProvider<EyeXFixationPoint> _dataProvider;

    /// <summary>
    /// Gets the last fixation.
    /// </summary>
    public EyeXFixationPoint LastFixation { get; private set; }

    protected void Awake()
    {
        _eyexHost = EyeXHost.GetInstance();
        _dataProvider = _eyexHost.GetFixationDataProvider(fixationDataMode);
    }

    protected void Update()
    {
        LastFixation = _dataProvider.Last;
    }

    protected void OnEnable()
    {
        _dataProvider.Start();
    }

    protected void OnDisable()
    {
        _dataProvider.Stop();
    }
}
