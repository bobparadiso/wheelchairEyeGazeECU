//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Tobii.EyeX.Client;
using System.Linq;

/// <summary>
/// Accesses and monitors enumerable engine states.
/// Used by the EyeXHost.
/// </summary>
/// <typeparam name="T">Data type of the engine state.</typeparam>
internal class EyeXEngineEnumerableStateAccessor<T> : EyeXEngineStateAccessor<T[]>
{
    public EyeXEngineEnumerableStateAccessor(string statePath)
        : base(statePath)
    {
    }

    /// <summary>
    /// Gets the data from the state bag.
    /// </summary>
    /// <param name="bag">The bag.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if data could be retrieved; otherwise <c>false</c>.</returns>
    protected override bool GetData(StateBag bag, out T[] value)
    {
        IEnumerable<T> enumerableValue;
        var result = bag.TryGetStateValueAsEnumerable<T>(out enumerableValue, StatePath);
        if(result)
        {
            value = enumerableValue.ToArray();
        }
        else
        {
            value = default(T[]);
        }
        return result;
    }
}