namespace Epiforge.Extensions.Components;

/// <summary>
/// Provides the disposal status of an object
/// </summary>
public interface IDisposalStatus
{
    /// <summary>
    /// Gets whether this object has been disposed
    /// </summary>
    bool IsDisposed { get; }
}
