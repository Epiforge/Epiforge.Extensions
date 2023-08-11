namespace Epiforge.Extensions.Components;

/// <summary>
/// Provides an overridable mechanism for releasing unmanaged resources asynchronously or synchronously
/// </summary>
public abstract class Disposable :
    PropertyChangeNotifier,
    IAsyncDisposable,
    IDisposable,
    INotifyDisposalOverridden,
    IDisposalStatus,
    INotifyDisposed,
    INotifyDisposing
{
    /// <summary>
    /// Finalizes this object
    /// </summary>
    [ExcludeFromCodeCoverage]
    ~Disposable()
    {
        if (loggerSetStackTrace is null)
            Logger?.LogWarning(EventIds.Epiforge_Extensions_Components_FinalizerCalled, "Finalizer called: did you forget to dispose an object? (set logging minimum level to Trace to see the stack trace for when the Logger was set)");
        else
            Logger?.LogWarning(EventIds.Epiforge_Extensions_Components_FinalizerCalled, "Finalizer called: did you forget to dispose an object? (stack trace for when the Logger was set: {LoggerSetStackTrace})", loggerSetStackTrace);
        var e = DisposalNotificationEventArgs.ByCallingFinalizer;
        OnDisposing(e);
        Dispose(false);
        IsDisposed = true;
        OnDisposed(e);
    }

    readonly AsyncLock disposalAccess = new();
    bool isDisposed;
    string? loggerSetStackTrace;

    /// <summary>
    /// Gets whether this object has been disposed
    /// </summary>
	public bool IsDisposed
    {
        get => isDisposed;
        private set => SetBackedProperty(ref isDisposed, in value, IsDisposedPropertyChanging, IsDisposedPropertyChanged);
    }

    /// <summary>
    /// Occurs when this object's disposal has been overridden
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;

    /// <summary>
    /// Occurs when this object has been disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposed;

    /// <summary>
    /// Occurs when this object is being disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposing;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public virtual void Dispose()
    {
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_DisposeCalled, "Dispose called");
        using (disposalAccess.Lock())
            if (!IsDisposed)
            {
                var e = DisposalNotificationEventArgs.ByCallingDispose;
                OnDisposing(e);
                if (IsDisposed = Dispose(true))
                {
                    OnDisposed(e);
                    GC.SuppressFinalize(this);
                }
                else
                    OnDisposalOverridden(e);
            }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
    /// <returns>true if disposal completed; otherwise, false</returns>
    protected abstract bool Dispose(bool disposing);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_DisposeCalled, "DisposeAsync called");
        using (await disposalAccess.LockAsync().ConfigureAwait(false))
            if (!IsDisposed)
            {
                var e = DisposalNotificationEventArgs.ByCallingDispose;
                OnDisposing(e);
                if (IsDisposed = await DisposeAsync(true).ConfigureAwait(false))
                {
                    OnDisposed(e);
                    GC.SuppressFinalize(this);
                }
                else
                    OnDisposalOverridden(e);
            }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
    /// <returns>true if disposal completed; otherwise, false</returns>
    protected abstract ValueTask<bool> DisposeAsync(bool disposing);

    /// <inheritdoc/>
    protected override void LoggerSet()
    {
        if (Logger?.IsEnabled(LogLevel.Trace) ?? false)
            loggerSetStackTrace = Environment.StackTrace;
    }

    void OnDisposalOverridden(DisposalNotificationEventArgs e)
    {
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisingDisposalOverridden, "Raising DisposalOverridden event");
        DisposalOverridden?.Invoke(this, e);
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisedDisposalOverridden, "Raised DisposalOverridden event");
    }

    void OnDisposed(DisposalNotificationEventArgs e)
    {
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisingDisposed, "Raising Disposed event");
        Disposed?.Invoke(this, e);
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisedDisposed, "Raised Disposed event");
    }

    void OnDisposing(DisposalNotificationEventArgs e)
    {
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisingDisposing, "Raising Disposing event");
        Disposing?.Invoke(this, e);
        Logger?.LogTrace(EventIds.Epiforge_Extensions_Components_RaisedDisposing, "Raised Disposing event");
    }

    /// <summary>
    /// Ensure the object has not been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException">The object has already been disposed</exception>
    protected void ThrowIfDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    internal static readonly PropertyChangedEventArgs IsDisposedPropertyChanged = new(nameof(IsDisposed));
    internal static readonly PropertyChangingEventArgs IsDisposedPropertyChanging = new(nameof(IsDisposed));
}
