namespace Epiforge.Extensions.Collections;

static class LoggingExtensions
{
    public static string ToStringForLogging(this NotifyCollectionChangedEventArgs e) =>
        e.Action switch
        {
            NotifyCollectionChangedAction.Add when e.NewItems is { } newItems => $"added {string.Join(", ", newItems.Cast<object>())} at index {e.NewStartingIndex}",
            NotifyCollectionChangedAction.Move when e.OldItems is { } oldItems => $"moved {string.Join(", ", oldItems.Cast<object>())} from index {e.OldStartingIndex} to index {e.NewStartingIndex}",
            NotifyCollectionChangedAction.Remove when e.OldItems is { } oldItems => $"removed {string.Join(", ", oldItems.Cast<object>())} at index {e.OldStartingIndex}",
            NotifyCollectionChangedAction.Replace when e.OldItems is { } oldItems && e.NewItems is { } newItems && e.OldStartingIndex == e.NewStartingIndex => $"replaced {string.Join(", ", oldItems.Cast<object>())} at index {e.OldStartingIndex} with {string.Join(", ", newItems.Cast<object>())}",
            NotifyCollectionChangedAction.Reset => "reset",
            _ => throw new NotSupportedException()
        };
}
