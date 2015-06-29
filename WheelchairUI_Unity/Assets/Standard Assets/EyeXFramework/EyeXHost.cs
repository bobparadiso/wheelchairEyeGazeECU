//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;
using Rect = UnityEngine.Rect;
using Environment = Tobii.EyeX.Client.Environment;

/// <summary>
/// Provides the main point of contact with the EyeX Engine. 
/// Hosts an EyeX context and responds to engine queries using a repository of interactors.
/// </summary>
[AddComponentMenu("")]
public partial class EyeXHost : MonoBehaviour
{
    /// <summary>
    /// If set to true, it will automatically initialize the EyeX Engine on Start().
    /// </summary>
    public bool initializeOnStart = true;

    /// <summary>
    /// Special interactor ID indicating that an interactor doesn't have a parent.
    /// </summary>
    public const string NoParent = Literals.RootId;

    private static EyeXHost _instance;

    private readonly object _lock = new object();
    private readonly Dictionary<string, IEyeXGlobalInteractor> _globalInteractors = new Dictionary<string, IEyeXGlobalInteractor>();
    private readonly Dictionary<string, EyeXInteractor> _interactors = new Dictionary<string, EyeXInteractor>();
    private readonly EyeXActivationHub _activationHub = new EyeXActivationHub();
	private readonly EyeXPanningHub _pannableHub = new EyeXPanningHub();
    private Environment _environment;
    private Context _context;
    private Vector2 _viewportPosition = new Vector2(float.NaN, float.NaN);
    private Vector2 _viewportPixelsPerDesktopPixel = Vector2.one;
    private bool _isConnected;
    private bool _isPaused;
    private bool _isFocused;
    private bool _runInBackground;
    private EyeXViewportBoundsProvider _viewportBoundsProvider;
    private Version _engineVersion;

    // Engine state accessors
    private EyeXEngineStateAccessor<Tobii.EyeX.Client.Rect> _screenBoundsStateAccessor;
    private EyeXEngineStateAccessor<Size2> _displaySizeStateAccessor;
    private EyeXEngineStateAccessor<EyeTrackingDeviceStatus> _eyeTrackingDeviceStatusStateAccessor;
    private EyeXEngineStateAccessor<UserPresence> _userPresenceStateAccessor;
	private EyeXEngineStateAccessor<GazeTracking> _gazeTracking;

    /// <summary>
    /// Gets the engine state: Screen bounds in pixels.
    /// </summary>
    public EyeXEngineStateValue<Tobii.EyeX.Client.Rect> ScreenBounds
    {
        get { return _screenBoundsStateAccessor.GetCurrentValue(_context); }
    }

    /// <summary>
    /// Gets the engine state: Display size, width and height, in millimeters.
    /// </summary>
    public EyeXEngineStateValue<Size2> DisplaySize
    {
        get { return _displaySizeStateAccessor.GetCurrentValue(_context); }
    }

    /// <summary>
    /// Gets the engine state: Eye tracking status.
    /// </summary>
    public EyeXDeviceStatus EyeTrackingDeviceStatus
    {
        get
        {
            return EnumHelpers.ConvertToEyeXDeviceStatus(
                _eyeTrackingDeviceStatusStateAccessor.GetCurrentValue(_context));
        }
    }

    /// <summary>
    /// Gets the engine state: User presence.
    /// </summary>
    public EyeXUserPresence UserPresence
    {
        get
        {
            return EnumHelpers.ConvertToEyeXUserPresence(
                _userPresenceStateAccessor.GetCurrentValue(_context));
        }
    }

	/// <summary>
	/// Gets the engine state: Gaze tracking.
	/// </summary>
	/// <value>The gaze tracking.</value>
	public EyeXGazeTracking GazeTracking
	{
		get 
		{
            return EnumHelpers.ConvertToEyeXGazeTracking(
                this, _gazeTracking.GetCurrentValue(_context));
		}
	}

    /// <summary>
    /// Gets the engine version.
    /// </summary>
    public Version EngineVersion
    {
        get { return _engineVersion; }
    }

    /// <summary>
    /// Gets the shared <see cref="IEyeXActivationHub"/> used for synchronizing activation events across interactors and frames.
    /// Use this object when creating <see cref="EyeXActivatable"/> behaviors.
    /// </summary>
    public IEyeXActivationHub ActivationHub
    {
        get { return _activationHub; }
    }

    /// <summary>
    /// Gets the shared <see cref="EyeXPannableHub"/> used for synchronizing activation events across interactors and frames.
    /// Use this object when creating <see cref="EyeXPannable"/> behaviors.
    /// </summary>
	public EyeXPanningHub PannableHub
    {
        get { return _pannableHub; }
    }

    /// <summary>
    /// Returns a value indicating whether The EyeX Engine has been initialized
    /// </summary>
    public bool IsInitialized
    {
        get { return _context != null; }
    }

	/// <summary>
	/// Gets a value indicating whether the host is running.
	/// </summary>
	/// <value><c>true</c> if the host is running; otherwise, <c>false</c>.</value>
    private bool IsRunning
    {
        get
        {
            return !_isPaused &&
                (_isFocused || _runInBackground);
        }
    }

    /// <summary>
    /// Gets the singleton EyeXHost instance.
    /// Users of this class should store a reference to the singleton instance in their Awake() method, or similar,
    /// to ensure that the EyeX host instance stays alive at least as long as the user object. Otherwise the
    /// EyeXHost might be garbage collected and replaced with a new, uninitialized instance during application 
    /// shutdown, and that would lead to unexpected behavior.
    /// </summary>
    /// <returns>The instance.</returns>
    public static EyeXHost GetInstance()
    {
        if (_instance == null)
        {
            // create a game object with a new instance of this class attached as a component.
            // (there's no need to keep a reference to the game object, because game objects are not garbage collected.)
            var container = new GameObject();
            container.name = "EyeXHostContainer";
            DontDestroyOnLoad(container);
            _instance = (EyeXHost)container.AddComponent(typeof(EyeXHost));
        }

        return _instance;
    }

    /// <summary>
    /// Initialize helper classes and state accessors on Awake
    /// </summary>
    void Awake()
    {
        _runInBackground = Application.runInBackground;

#if UNITY_EDITOR
        _viewportBoundsProvider = CreateEditorScreenHelper();
#else
        _viewportBoundsProvider = new UnityPlayerViewportBoundsProvider();
#endif

        _screenBoundsStateAccessor = new EyeXEngineStateAccessor<Tobii.EyeX.Client.Rect>(StatePaths.EyeTrackingScreenBounds);
        _displaySizeStateAccessor = new EyeXEngineStateAccessor<Size2>(StatePaths.EyeTrackingDisplaySize);
        _eyeTrackingDeviceStatusStateAccessor = new EyeXEngineStateAccessor<EyeTrackingDeviceStatus>(StatePaths.EyeTrackingState);
        _userPresenceStateAccessor = new EyeXEngineStateAccessor<UserPresence>(StatePaths.UserPresence);
        _userProfileNameStateAccessor = new EyeXEngineStateAccessor<string>(StatePaths.ProfileName);
        _userProfileNamesStateAccessor = new EyeXEngineEnumerableStateAccessor<string>(StatePaths.EyeTrackingProfiles);
		_gazeTracking = new EyeXEngineStateAccessor<GazeTracking>(StatePaths.GazeTracking);
    }

#if UNITY_EDITOR
    private static EyeXViewportBoundsProvider CreateEditorScreenHelper()
    {
#if UNITY_4_5 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1
        return new LegacyEditorViewportBoundsProvider();
#else
        return new EditorViewportBoundsProvider();
#endif
    }
#endif

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
    /// </summary>
    public void Start()
    {
        if (initializeOnStart)
        {
            InitializeEyeX();
        }
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    public void Update()
    {
        if (_engineVersion == null && IsInitialized && _isConnected)
        {
            _engineVersion = GetEngineVersion();
        }

        // update the viewport position, in case the game window has been moved or resized.
        var viewportBounds = _viewportBoundsProvider.GetViewportPhysicalBounds();
        _viewportPosition = new Vector2(viewportBounds.x, viewportBounds.y);
        _viewportPixelsPerDesktopPixel = new Vector2(Screen.width / viewportBounds.width, Screen.height / viewportBounds.height);

        StartCoroutine(DoEndOfFrameCleanup());
    }

    private IEnumerator DoEndOfFrameCleanup()
    {
        yield return new WaitForEndOfFrame();
        _activationHub.EndFrame();
        _pannableHub.EndFrame();
    }

    /// <summary>
    /// Sent to all game objects when the player gets or loses focus.
    /// </summary>
    /// <param name="focusStatus">Gets a value indicating whether the player is focused.</param>
    public void OnApplicationFocus(bool focusStatus)
    {
        var wasRunning = IsRunning;
        _isFocused = focusStatus;

        // make sure that data streams are disabled while the game is paused.
        if (wasRunning != IsRunning && _isConnected)
        {
            CommitAllGlobalInteractors();
        }
    }

    /// <summary>
    /// Sent to all game objects when the player pauses.
    /// </summary>
    /// <param name="pauseStatus">Gets a value indicating whether the player is paused.</param>
    public void OnApplicationPause(bool pauseStatus)
    {
        var wasRunning = IsRunning;
        _isPaused = pauseStatus;

        // make sure that data streams are disabled while the game is paused.
        if (wasRunning != IsRunning && _isConnected)
        {
            CommitAllGlobalInteractors();
        }
    }

    /// <summary>
    /// Sent to all game objects before the application is quit.
    /// </summary>
    public void OnApplicationQuit()
    {
        ShutdownEyeX();
    }

    /// <summary>
    /// Registers an interactor with the repository.
    /// </summary>
    /// <param name="interactor">The interactor.</param>
    public void RegisterInteractor(EyeXInteractor interactor)
    {
        lock (_lock)
        {
            _interactors[interactor.Id] = interactor;
        }
    }

    /// <summary>
    /// Gets an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    /// <returns>Interactor, or null if not found.</returns>
    public EyeXInteractor GetInteractor(string interactorId)
    {
        lock (_lock)
        {
            EyeXInteractor interactor = null;
            _interactors.TryGetValue(interactorId, out interactor);
            return interactor;
        }
    }

    /// <summary>
    /// Removes an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    public void UnregisterInteractor(string interactorId)
    {
        lock (_lock)
        {
            _interactors.Remove(interactorId);
        }
    }

    /// <summary>
    /// Gets a provider of gaze point data.
    /// See <see cref="IEyeXDataProvider{T}"/>.
    /// </summary>
    /// <param name="mode">Specifies the kind of data processing to be applied by the EyeX Engine.</param>
    /// <returns>The data provider.</returns>
    public IEyeXDataProvider<EyeXGazePoint> GetGazePointDataProvider(GazePointDataMode mode)
    {
        var dataStream = new EyeXGazePointDataStream(mode);
        return GetDataProviderForDataStream<EyeXGazePoint>(dataStream);
    }

    /// <summary>
    /// Gets a provider of fixation data.
    /// See <see cref="IEyeXDataProvider{T}"/>.
    /// </summary>
    /// <param name="mode">Specifies the kind of data processing to be applied by the EyeX Engine.</param>
    /// <returns>The data provider.</returns>
    public IEyeXDataProvider<EyeXFixationPoint> GetFixationDataProvider(FixationDataMode mode)
    {
        var dataStream = new EyeXFixationDataStream(mode);
        return GetDataProviderForDataStream<EyeXFixationPoint>(dataStream);
    }

    /// <summary>
    /// Gets a provider of eye position data.
    /// See <see cref="IEyeXDataProvider{T}"/>.
    /// </summary>
    /// <returns>The data provider.</returns>
    public IEyeXDataProvider<EyeXEyePosition> GetEyePositionDataProvider()
    {
        var dataStream = new EyeXEyePositionDataStream();
        return GetDataProviderForDataStream<EyeXEyePosition>(dataStream);
    }

    /// <summary>
    /// Trigger an activation ("direct click").
    /// Use this method if you want to bind the click command to a key other than the one used 
    /// in the EyeX Interaction settings -- or to something other than a key press event.
    /// </summary>
    public void TriggerActivation()
    {
        _context.CreateActionCommand(ActionType.Activate)
            .ExecuteAsync(null);
    }

    /// <summary>
    /// Gets a data provider for a given data stream: preferably an existing one 
    /// in the _globalInteractors collection, or, failing that, the one passed 
    /// in as a parameter.
    /// </summary>
    /// <typeparam name="T">Type of the provided data value object.</typeparam>
    /// <param name="dataStream">Data stream to be added.</param>
    /// <returns>A data provider.</returns>
    private IEyeXDataProvider<T> GetDataProviderForDataStream<T>(EyeXDataStreamBase<T> dataStream)
    {
        lock (_lock)
        {
            IEyeXGlobalInteractor existing;
            if (_globalInteractors.TryGetValue(dataStream.Id, out existing))
            {
                return (IEyeXDataProvider<T>)existing;
            }

            _globalInteractors.Add(dataStream.Id, dataStream);
            dataStream.Updated += OnGlobalInteractorUpdated;
            return dataStream;
        }
    }

    /// <summary>
    /// Gets an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    /// <returns>Interactor, or null if not found.</returns>
    private IEyeXGlobalInteractor GetGlobalInteractor(string interactorId)
    {
        lock (_lock)
        {
            IEyeXGlobalInteractor interactor = null;
            _globalInteractors.TryGetValue(interactorId, out interactor);
            return interactor;
        }
    }

    /// <summary>
    /// Initializes the EyeX engine.
    /// </summary>
    public void InitializeEyeX()
    {
        if (IsInitialized) return;

        try
        {
            Tobii.EyeX.Client.Interop.EyeX.EnableMonoCallbacks("mono");
            _environment = Environment.Initialize();
        }
        catch (InteractionApiException ex)
        {
            Debug.LogError("EyeX initialization failed: " + ex.Message);
        }
        catch (DllNotFoundException)
        {
#if UNITY_EDITOR
            Debug.LogError("EyeX initialization failed because the client access library 'Tobii.EyeX.Client.dll' could not be loaded. " +
                "Please make sure that it is present in the Unity project directory. " +
                "You can find it in the SDK package, in the lib/x86 directory. (Currently only Windows is supported.)");
#else
			Debug.LogError("EyeX initialization failed because the client access library 'Tobii.EyeX.Client.dll' could not be loaded. " +
				"Please make sure that it is present in the root directory of the game/application.");
#endif
            return;
        }

        try
        {
            _context = new Context(false);
            _context.RegisterQueryHandlerForCurrentProcess(HandleQuery);
            _context.RegisterEventHandler(HandleEvent);
            _context.ConnectionStateChanged += OnConnectionStateChanged;
            _context.EnableConnection();

            print("EyeX is running.");
        }
        catch (InteractionApiException ex)
        {
            Debug.LogError("EyeX context initialization failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Shuts down the eyeX engine.
    /// </summary>
    public void ShutdownEyeX()
    {
        if (!IsInitialized) return;
        print("EyeX is shutting down.");

        if (_context != null)
        {
            // The context must be shut down before disposing.
            try
            {
                _context.Shutdown(1000, false);
            }
            catch (InteractionApiException ex)
            {
                Debug.LogError("EyeX context shutdown failed: " + ex.Message);
            }

            _context.Dispose();
            _context = null;
        }

        if (_environment != null)
        {
            _environment.Dispose();
            _environment = null;
        }
    }

    private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
    {
        if (e.State == ConnectionState.Connected)
        {
            _isConnected = true;

            // commit the snapshot with the global interactor as soon as the connection to the engine is established.
            // (it cannot be done earlier because committing means "send to the engine".)
            CommitAllGlobalInteractors();

            _screenBoundsStateAccessor.OnConnected(_context);
            _displaySizeStateAccessor.OnConnected(_context);
            _eyeTrackingDeviceStatusStateAccessor.OnConnected(_context);
            _userPresenceStateAccessor.OnConnected(_context);
            _userProfileNameStateAccessor.OnConnected(_context);
            _userProfileNamesStateAccessor.OnConnected(_context);
			_gazeTracking.OnConnected(_context);
        }
        else
        {
            _isConnected = false;

            _screenBoundsStateAccessor.OnDisconnected();
            _displaySizeStateAccessor.OnDisconnected();
            _eyeTrackingDeviceStatusStateAccessor.OnDisconnected();
            _userPresenceStateAccessor.OnDisconnected();
            _userProfileNameStateAccessor.OnDisconnected();
            _userProfileNamesStateAccessor.OnDisconnected();
			_gazeTracking.OnDisconnected();
        }
    }

    private void HandleQuery(Query query)
    {
        // NOTE: this method is called from a worker thread, so it must not access any game objects.
        using (query)
        {
            try
            {
                Rect queryRectInGuiCoordinates;
                if (!TryGetQueryRectangle(query, out queryRectInGuiCoordinates)) { return; }

                // Make a copy of the collection of interactors to avoid race conditions.
                List<EyeXInteractor> interactorsCopy;
                lock (_lock)
                {
                    interactorsCopy = new List<EyeXInteractor>(_interactors.Values);
                }

                // Create the snapshot and add the interactors that intersect with the query bounds.
                using (var snapshot = _context.CreateSnapshotWithQueryBounds(query))
                {
                    snapshot.AddWindowId(_viewportBoundsProvider.GameWindowId);
                    foreach (var interactor in interactorsCopy)
                    {
                        if (interactor.IntersectsWith(queryRectInGuiCoordinates))
                        {
                            interactor.AddToSnapshot(
                                snapshot,
                                _viewportBoundsProvider.GameWindowId,
                                _viewportPosition,
                                _viewportPixelsPerDesktopPixel);
                        }
                    }

                    CommitSnapshot(snapshot);
                }
            }
            catch (InteractionApiException ex)
            {
                print("EyeX query handler failed: " + ex.Message);
            }
        }
    }

    private bool TryGetQueryRectangle(Query query, out Rect queryRectInGuiCoordinates)
    {
        if (float.IsNaN(_viewportPosition.x))
        {
            // We don't have a valid game window position, so we cannot respond to any queries at this time.
            queryRectInGuiCoordinates = new Rect();
            return false;
        }

        double boundsX, boundsY, boundsWidth, boundsHeight; // desktop pixels
        using (var bounds = query.Bounds)
        {
            if (!bounds.TryGetRectangularData(out boundsX, out boundsY, out boundsWidth, out boundsHeight))
            {
                queryRectInGuiCoordinates = new Rect();
                return false;
            }
        }

        queryRectInGuiCoordinates = new Rect(
            (float)((boundsX - _viewportPosition.x) * _viewportPixelsPerDesktopPixel.x),
            (float)((boundsY - _viewportPosition.y) * _viewportPixelsPerDesktopPixel.y),
            (float)(boundsWidth * _viewportPixelsPerDesktopPixel.x),
            (float)(boundsHeight * _viewportPixelsPerDesktopPixel.y));

        return true;
    }

    private void HandleEvent(InteractionEvent event_)
    {
        // NOTE: this method is called from a worker thread, so it must not access any game objects.
        using (event_)
        {
            try
            {
                // Route the event to the appropriate interactor, if any.
                var interactorId = event_.InteractorId;
                var globalInteractor = GetGlobalInteractor(interactorId);
                if (globalInteractor != null)
                {
                    globalInteractor.HandleEvent(event_, _viewportPosition, _viewportPixelsPerDesktopPixel);
                }
                else
                {
                    var interactor = GetInteractor(interactorId);
                    if (interactor != null)
                    {
                        interactor.HandleEvent(event_);
                    }
                }
            }
            catch (InteractionApiException ex)
            {
                print("EyeX event handler failed: " + ex.Message);
            }
        }
    }

    private void OnGlobalInteractorUpdated(object sender, EventArgs e)
    {
        var globalInteractor = (IEyeXGlobalInteractor)sender;

        if (_isConnected)
        {
            CommitGlobalInteractors(new[] { globalInteractor });
        }
    }

    private void CommitAllGlobalInteractors()
    {
        // make a copy of the collection of interactors to avoid race conditions.
        List<IEyeXGlobalInteractor> globalInteractorsCopy;
        lock (_lock)
        {
            if (_globalInteractors.Count == 0) { return; }

            globalInteractorsCopy = new List<IEyeXGlobalInteractor>(_globalInteractors.Values);
        }

        CommitGlobalInteractors(globalInteractorsCopy);
    }

    private void CommitGlobalInteractors(IEnumerable<IEyeXGlobalInteractor> globalInteractors)
    {
        try
        {
            var snapshot = CreateGlobalInteractorSnapshot();
            var forceDeletion = !IsRunning;
            foreach (var globalInteractor in globalInteractors)
            {
                globalInteractor.AddToSnapshot(snapshot, forceDeletion);
            }

            CommitSnapshot(snapshot);
        }
        catch (InteractionApiException ex)
        {
            print("EyeX operation failed: " + ex.Message);
        }
    }

    private Snapshot CreateGlobalInteractorSnapshot()
    {
        var snapshot = _context.CreateSnapshot();
        snapshot.CreateBounds(BoundsType.None);
        snapshot.AddWindowId(Literals.GlobalInteractorWindowId);
        return snapshot;
    }

    private void CommitSnapshot(Snapshot snapshot)
    {
#if DEVELOPMENT_BUILD
		snapshot.CommitAsync(OnSnapshotCommitted);
#else
        snapshot.CommitAsync(null);
#endif
    }

#if DEVELOPMENT_BUILD
	private static void OnSnapshotCommitted(AsyncData asyncData)
	{
		try
		{
			ResultCode resultCode;
			if (!asyncData.TryGetResultCode(out resultCode)) { return; }

			if (resultCode == ResultCode.InvalidSnapshot)
			{
				print("Snapshot validation failed: " + GetErrorMessage(asyncData));
			}
			else if (resultCode != ResultCode.Ok && resultCode != ResultCode.Cancelled)
			{
				print("Could not commit snapshot: " + GetErrorMessage(asyncData));
			}
		}
		catch (InteractionApiException ex)
		{
			print("EyeX operation failed: " + ex.Message);
		}

		asyncData.Dispose();
	}

	private static string GetErrorMessage(AsyncData asyncData)
	{
		string errorMessage;
		if (asyncData.TryGetPropertyValue<string>(Literals.ErrorMessage, out errorMessage))
		{
			return errorMessage;
		}
		else
		{
			return "Unspecified error.";
		}
	}
#endif

    public Version GetEngineVersion()
    {
        if (_context == null)
        {
            throw new InvalidOperationException("The EyeX host has not been started.");
        }
        if(_engineVersion != null)
        {
            return _engineVersion;
        }
        var stateBag = _context.GetState(StatePaths.EngineVersion);
        string value;
        if (!stateBag.TryGetStateValue(out value, StatePaths.EngineVersion))
        {
            throw new InvalidOperationException("Could not get engine version.");
        }
        return new Version(value);
    }
}
