//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that encapsulates the <see cref="EyeXGazeAware"/> behavior.
/// </summary>
[AddComponentMenu("Tobii EyeX/Gaze Aware")]
public class GazeAwareComponent : EyeXGameObjectInteractorBase
{
    // Delay between first glance and gaze aware event response
    public int delayInMilliseconds;

    /// <summary>
    /// Gets a value indicating whether the user's eye-gaze is within the bounds of the interactor.
    /// </summary>
    public bool HasGaze { get; private set; }

    protected override void Update()
    {
        base.Update();

        HasGaze = GameObjectInteractor.HasGaze();
    }

    protected override IList<IEyeXBehavior> GetEyeXBehaviorsForGameObjectInteractor()
    {
        return new List<IEyeXBehavior>(new[] { new EyeXGazeAware() { DelayTime = delayInMilliseconds }});
    }
}
