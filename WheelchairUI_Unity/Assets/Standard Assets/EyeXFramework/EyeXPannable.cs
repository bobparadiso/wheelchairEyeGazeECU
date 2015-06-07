//-----------------------------------------------------------------------
// Copyright 2015 Tobii AB (publ). All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Used for assigning the Pannable behavior to an interactor, making it respond to panning events.
/// See <see cref="EyeXInteractor.EyeXBehaviors"/>.
/// </summary>
public sealed class EyeXPannable : IEyeXBehavior
{
	private readonly EyeXPanningHub _hub;

    /// <summary>
    /// Gets or sets the available panning directions.
    /// This could be set only once, but it is also possible to keep this up-to-date
    /// dynamically if for example the up-direction is temporarily not available
    /// because one is already viewing the topmost part of some pannable contents.
    /// </summary>
    public EyeXPanDirection AvailablePanDirections { get; set; }

    /// <summary>
    /// Gets or sets the current panning profile.
    /// The available panning profiles in <see cref="EyeXPanningProfile"/> are 
    /// optimized for different kinds of panning and scrolling. The panning
    /// profile decides the velocities to trigger at different parts of the 
    /// pannable area, like for example where to switch from one direction to
    /// another.
    /// </summary>
    public EyeXPanningProfile Profile { get; set; }

    /// <summary>
    /// Gets the current panning velocity in physical pixels per second.
    /// </summary>
    public Vector2 Velocity { get; internal set; }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="hub">The hub.</param>
    /// <param name="availablePanDirections">The directions available for panning.</param>
    /// <param name="profile">The profile.</param>
	public EyeXPannable(EyeXPanningHub hub, EyeXPanDirection availablePanDirections, EyeXPanningProfile profile)
    {
        _hub = hub;

        AvailablePanDirections = availablePanDirections;
        Profile = profile;        
        Velocity = Vector2.zero;
    }

	#region IEyeXBehavior interface

	public void AssignBehavior (Interactor interactor)
	{
		// Create the parameters used to create the pannable behavior.
		var param = new PannableParams
		{
			IsHandsFreeEnabled = EyeXBoolean.False,
			PanDirectionsAvailable = (PanDirection)AvailablePanDirections,
			Profile = (PanningProfile)Profile
		};
		
		// Create and associate the pannable behavior with the interactor.
		interactor.CreatePannableBehavior(ref param);
	}

	public void HandleEvent (string interactorId, IEnumerable<Behavior> behaviors)
	{
		_hub.Handle(this, behaviors);
	}

	#endregion
}

[Flags]
public enum EyeXPanDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Up = 4,
    Down = 8,
    All = Left | Right | Up | Down
}

public enum EyeXPanningProfile
{
    None = 1,
    Reading = 2,
    Horizontal = 3,
    Vertical = 4,
    VerticalFirstThenHorizontal = 5,
    Radial = 6,
    HorizontalFirstThenVertical = 7,
}

public static class EyeXPannableInteractorExtensions
{
    /// <summary>
    /// Gets the current panning velocity.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public static Vector2 GetPanVelocity(this EyeXInteractor interactor)
    {
        var behavior = GetPannableBehavior(interactor);
        return behavior == null ? Vector2.zero : behavior.Velocity;
    }

    /// <summary>
    /// Gets the <see cref="EyeXPannable"/> behavior for the specified interactor.
    /// </summary>
    /// <param name="interactor">The interactor.</param>
    /// <returns>The <see cref="EyeXPannable"/> for the specified interactor.</returns>
    public static EyeXPannable GetPannableBehavior(EyeXInteractor interactor)
    {
        foreach (var behavior in interactor.EyeXBehaviors)
        {
            var gazeAwareBehavior = behavior as EyeXPannable;
            if (gazeAwareBehavior != null)
            {
                return gazeAwareBehavior;
            }
        }
        return null;
    }
}