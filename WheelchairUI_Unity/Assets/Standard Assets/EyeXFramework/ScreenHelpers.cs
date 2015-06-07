//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Provides functions related to viewport bounds resolution.
/// </summary>
internal abstract class EyeXViewportBoundsProvider
{
    protected IntPtr _hwnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="EyeXViewportBoundsProvider"/> class.
    /// </summary>
    protected EyeXViewportBoundsProvider()
    {
        _hwnd = FindWindowWithThreadProcessId();
        GameWindowId = _hwnd.ToString();
    }

    /// <summary>
    /// Gets the Window ID for the game window.
    /// </summary>
    public string GameWindowId { get; private set; }

    /// <summary>
    /// Gets the position of the viewport in desktop coordinates (physical pixels).
    /// </summary>
    /// <returns>Position in physical desktop pixels.</returns>
    public Rect GetViewportPhysicalBounds()
    {
        return LogicalToPhysical(GetViewportLogicalBounds());
    }

    /// <summary>
    /// Gets the position of the viewport in logical pixels.
    /// </summary>
    /// <returns>Position in logical pixels.</returns>
    protected abstract Rect GetViewportLogicalBounds();

    /// <summary>
    /// Maps from logical pixels to physical desktop pixels.
    /// </summary>
    /// <param name="rect">Rectangle to be transformed.</param>
    /// <returns>Transformed rectangle.</returns>
    protected virtual Rect LogicalToPhysical(Rect rect)
    {
        var topLeft = new Win32Helpers.POINT { x = (int)rect.x, y = (int)rect.y };
        Win32Helpers.LogicalToPhysicalPoint(_hwnd, ref topLeft);

        var bottomRight = new Win32Helpers.POINT { x = (int)(rect.x + rect.width), y = (int)(rect.y + rect.height) };
        Win32Helpers.LogicalToPhysicalPoint(_hwnd, ref bottomRight);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    /// <summary>
    /// Finds the window associated with the current thread and process.
    /// </summary>
    /// <returns>A window handle represented as a <see cref="IntPtr"/>.</returns>
    protected virtual IntPtr FindWindowWithThreadProcessId()
    {
        var processId = Process.GetCurrentProcess().Id;
        return WindowHelpers.FindWindowWithThreadProcessId(processId);
    }

    /// <summary>
    /// Gets the (Unity) screen size.
    /// </summary>
    /// <returns></returns>
    protected virtual Vector2 GetScreenSize()
    {
        return new Vector2(Screen.width, Screen.height);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Gets the Unity toolbar height.
    /// </summary>
    /// <returns></returns>
    protected virtual float GetToolbarHeight()
    {
        try
        {
            return UnityEditor.EditorStyles.toolbar.fixedHeight;
        }
        catch (NullReferenceException)
        {
            return 0f;
        }
    }

    /// <summary>
    /// Gets the Unity game view.
    /// </summary>
    /// <returns></returns>
    protected virtual UnityEditor.EditorWindow GetMainGameView()
    {
        var unityEditorType = Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Diagnostics.Debug.Assert(unityEditorType != null);
        var getMainGameViewMethod = unityEditorType.GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
        System.Diagnostics.Debug.Assert(getMainGameViewMethod != null);
        var result = getMainGameViewMethod.Invoke(null, null);
        return (UnityEditor.EditorWindow)result;
    }
#endif
}

/// <summary>
/// Provides utility functions related to screen and window handling within the Unity Player.
/// </summary>
internal class UnityPlayerViewportBoundsProvider : EyeXViewportBoundsProvider
{
    protected override Rect GetViewportLogicalBounds()
    {
        var clientRect = new Win32Helpers.RECT();
        Win32Helpers.GetClientRect(_hwnd, ref clientRect);

        var topLeft = new Win32Helpers.POINT();
        Win32Helpers.ClientToScreen(_hwnd, ref topLeft);

        var bottomRight = new Win32Helpers.POINT { x = clientRect.right, y = clientRect.bottom };
        Win32Helpers.ClientToScreen(_hwnd, ref bottomRight);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }
}

#if UNITY_EDITOR
/// <summary>
/// This class is used to resolve the editor viewport bounds in 
/// Unity versions previous to 4.6.
/// </summary>
internal class LegacyEditorViewportBoundsProvider : EyeXViewportBoundsProvider
{
    private UnityEditor.EditorWindow _gameWindow;
    private bool _initialized;

    private void Initialize()
    {
        _gameWindow = GetMainGameView();
        _initialized = true;
    }

    /// <summary>
    /// Gets the editor position of the editor viewport in logical pixels.
    /// </summary>
    /// <returns>The editor position of the viewport in logical pixels.</returns>
    protected override Rect GetViewportLogicalBounds()
    {
        if (!_initialized)
        {
            Initialize();
        }

        var gameWindowBounds = _gameWindow.position;

        // Adjust for the toolbar
        var toolbarHeight = GetToolbarHeight();
        gameWindowBounds.y += toolbarHeight;
        gameWindowBounds.height -= toolbarHeight;

        // Get the screen size.
        var screenSize = GetScreenSize();

        // Adjust for unused areas caused by fixed aspect ratio or resolution vs game window size mismatch
        var viewportOffsetX = (gameWindowBounds.width - screenSize.x) / 2.0f;
        var viewportOffsetY = (gameWindowBounds.height - screenSize.y) / 2.0f;

        return new Rect(
            gameWindowBounds.x + viewportOffsetX,
            gameWindowBounds.y + viewportOffsetY,
            screenSize.x,
            screenSize.y);
    }
}

/// <summary>
/// This class is used to resolve the editor viewport bounds for 
/// Unity version 4.6 and above.
/// </summary>
internal class EditorViewportBoundsProvider : EyeXViewportBoundsProvider
{
    private UnityEditor.EditorWindow _gameWindow;
    private Func<Rect> _windowParentBoundsAccessor;
    private float _toolbarHeight;    
    private bool _initialized;

    private const float TabHeight = 19f;

    private void Initialize()
    {
        _gameWindow = GetMainGameView();
        _windowParentBoundsAccessor = GetWindowParentBoundsAccessor(_gameWindow);
        _toolbarHeight = GetToolbarHeight();        
        _initialized = true;
    }

    /// <summary>
    /// Gets the editor position of the editor viewport in logical pixels.
    /// </summary>
    /// <returns>The editor position of the viewport in logical pixels.</returns>
    protected override Rect GetViewportLogicalBounds()
    {
        if (!_initialized)
        {
            Initialize();
        }

        var gameBounds = _gameWindow.position;
        var windowPosition = GetWindowPosition();
        var parentBounds = _windowParentBoundsAccessor();

        // Get the viewport's logical size.
        var screenSize = GetScreenSize();
        var viewportWidth = screenSize.x;
        var viewportHeight = screenSize.y;

        // Depending on whether or not the game window is nested in a panel or not,
        // the viewport's logical x position need to be calculated differently.
        // If the viewport is nested, we need to take the parent panel's x-position into account.
        var isNestedInPanel = gameBounds.x < parentBounds.x;
        var viewportX = isNestedInPanel
            ? windowPosition.x + parentBounds.x + gameBounds.x
            : windowPosition.x + gameBounds.x;

        // Calculate the viewport's logical y position.
        var viewportY = parentBounds.y + windowPosition.y + (_toolbarHeight + TabHeight);

        // Adjust for unused areas caused by fixed aspect ratio or resolution vs game window size mismatch.
        var offsetX = (gameBounds.width - viewportWidth) / 2.0f;
        var offsetY = (gameBounds.height - _toolbarHeight - viewportHeight) / 2.0f;

        return new Rect(viewportX + offsetX, viewportY + offsetY, viewportWidth, viewportHeight);
    }

    /// <summary>
    /// Gets an accessor to the parent panel's bounds accessor.
    /// </summary>
    /// <param name="gameView">The game view.</param>
    /// <returns>An accessor to the parent panel's bounds accessor</returns>
    protected virtual Func<Rect> GetWindowParentBoundsAccessor(UnityEditor.EditorWindow gameView)
    {
        System.Diagnostics.Debug.Assert(gameView != null);
        var parentHostField = gameView.GetType().GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (parentHostField != null)
        {
            var parentHost = parentHostField.GetValue(gameView);
            if (parentHost != null)
            {
                var windowPositionProperty = parentHost.GetType().GetProperty("windowPosition", BindingFlags.Public | BindingFlags.Instance);
                if (windowPositionProperty != null)
                {
                    // Create a delegate to get the parent host bounds.
                    return (Func<Rect>)
                        Delegate.CreateDelegate(typeof(Func<Rect>), parentHost,
                        windowPositionProperty.GetGetMethod());
                }
            }
        }
        throw new InvalidOperationException("Could not resolve window parent position accessor.");
    }

    /// <summary>
    /// Gets the gameview's window position.
    /// </summary>
    /// <returns></returns>
    protected virtual Vector2 GetWindowPosition()
    {
        var windowPosition = new Win32Helpers.POINT();
        Win32Helpers.ClientToScreen(_hwnd, ref windowPosition);
        return new Vector2(windowPosition.x, windowPosition.y);
    }
}
#endif