//-----------------------------------------------------------------------
// Copyright 2015 Tobii AB (publ). All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Aggregates panning related events from the EyeX Engine so that they appear consistently within rendering frames.
/// </summary>
public class EyeXPanningHub
{
    private readonly Dictionary<EyeXPannable, Vector2> _velocities;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public EyeXPanningHub()
    {
        _velocities = new Dictionary<EyeXPannable, Vector2>();
    }

    /// <summary>
    /// Handles an event belonging to <param name="owner">the specified owner</param>.
    /// </summary>
    /// <param name="owner">The owner of the event.</param>
    /// <param name="behaviors">The behaviors to be handled</param>
    public void Handle(EyeXPannable owner, IEnumerable<Behavior> behaviors)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.BehaviorType != BehaviorType.Pannable)
            {
                continue;
            }

            PannableEventType eventType;
            if (behavior.TryGetPannableEventType(out eventType))
            {
                if (eventType == PannableEventType.Pan)
                {
                    PannablePanEventParams param;
                    if (behavior.TryGetPannablePanEventParams(out param))
                    {
                        _velocities[owner] = new Vector2(
                            (float)param.PanVelocityX,
                            (float)param.PanVelocityY);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called at the end of each frame.
    /// </summary>
    public void EndFrame()
    {
        if (_velocities.Count > 0)
        {
            foreach (var velocity in _velocities)
            {
                var owner = velocity.Key;
                owner.Velocity = velocity.Value;
            }
            _velocities.Clear();
        }
    }
}
