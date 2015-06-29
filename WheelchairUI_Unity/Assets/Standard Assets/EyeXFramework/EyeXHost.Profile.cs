//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using Tobii.EyeX.Framework;

public partial class EyeXHost
{
    private EyeXEngineStateAccessor<string> _userProfileNameStateAccessor;
    private EyeXEngineEnumerableStateAccessor<string> _userProfileNamesStateAccessor;

    /// <summary>
    /// Gets the engine state: Current user profile.
    /// </summary>
    public EyeXEngineStateValue<string> UserProfileName
    {
        get
        {
            return _userProfileNameStateAccessor.GetCurrentValue(_context);
        }
    }

    /// <summary>
    /// Gets the engine state: User profiles.
    /// </summary>
    public EyeXEngineStateValue<string[]> UserProfileNames
    {
        get
        {
            return _userProfileNamesStateAccessor.GetCurrentValue(_context);
        }
    }

    public void SetCurrentProfile(string profileName)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("The EyeX host has not been started.");
        }
        _context.SetCurrentProfile(profileName, null);
    }
}