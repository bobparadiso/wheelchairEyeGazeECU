//-----------------------------------------------------------------------
// Copyright 2015 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;

/// <summary>
/// Contains utility functions for window handling.
/// </summary>
public static class WindowHelpers
{
    /// <summary>
    /// Shows the current window.
    /// </summary>
    public static void ShowCurrentWindow()
    {
        IntPtr hwnd = FindWindowWithThreadProcessId(Process.GetCurrentProcess().Id);
        Win32Helpers.ShowWindowAsync(hwnd, Win32Helpers.SW_SHOWDEFAULT);
    }

    internal static IntPtr FindWindowWithThreadProcessId(int processId)
    {
        var window = new IntPtr();

        Win32Helpers.EnumWindows(delegate(IntPtr wnd, IntPtr param)
        {
            var windowProcessId = 0;
            Win32Helpers.GetWindowThreadProcessId(wnd, out windowProcessId);
            if (windowProcessId == processId)
            {
                window = wnd;
                return false;
            }

            return true;
        },
        IntPtr.Zero);

        if (window.Equals(IntPtr.Zero))
        {
            UnityEngine.Debug.LogError("Could not find any window with process id " + processId);
        }

        return window;
    }

}
