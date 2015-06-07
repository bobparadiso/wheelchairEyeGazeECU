//-----------------------------------------------------------------------
// Copyright 2015 Tobii AB (publ). All rights reserved.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that encapsulates the <see cref="EyeXPannable"/> behavior.
/// </summary>
[AddComponentMenu("Tobii EyeX/Pannable")]
public class PannableComponent : EyeXGameObjectInteractorBase
{
    /// <summary>
    /// Available panning directions. Can be used to restrict the directions
    /// available in the panning profile.
    /// </summary>
    public EyeXPanDirection availableDirections = EyeXPanDirection.All;

    /// <summary>
    /// The panning profile decides how the panned area will be panned in
    /// different directions. 
    /// </summary>
    public EyeXPanningProfile profile = EyeXPanningProfile.Radial;

    /// <summary>
    /// Gets the last panning velocity in physical pixels per second.
    /// </summary>
    public Vector2 Velocity { get; private set; }

    protected override void Update()
    {
        base.Update();
		        
        Velocity = GameObjectInteractor.GetPanVelocity();
    }

    protected override IList<IEyeXBehavior> GetEyeXBehaviorsForGameObjectInteractor()
    {
        return new List<IEyeXBehavior>(new[] { new EyeXPannable(Host.PannableHub, availableDirections, profile) });
    }

    protected override bool AllowOverlap()
    {
        return true;
    }
}
