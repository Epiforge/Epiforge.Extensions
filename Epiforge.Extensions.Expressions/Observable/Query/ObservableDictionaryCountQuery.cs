namespace Epiforge.Extensions.Expressions.Observable.Query;

sealed class ObservableDictionaryCountQuery<TKey, TValue> :
    ObservableDictionaryScalarQuery<TKey, TValue, int>
    where TKey : notnull
{
    public ObservableDictionaryCountQuery(CollectionObserver collectionObserver, ObservableDictionaryQuery<TKey, TValue> observableDictionaryQuery) :
        base(collectionObserver, observableDictionaryQuery)
    {
    }

    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            var removedFromCache = observableDictionaryQuery.QueryDisposed(this);
            if (removedFromCache)
                observableDictionaryQuery.DictionaryChanged -= ObservableDictionaryQueryDictionaryChanged;
            return removedFromCache;
        }
        return true;
    }

    void Evaluate()
    {
        if (observableDictionaryQuery.OperationFault is { } fault)
            Evaluation = (fault, default);
        else
            Evaluation = (null, observableDictionaryQuery.Count);
    }

    void ObservableDictionaryQueryDictionaryChanged(object? sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        Evaluate();

    protected override void OnInitialization()
    {
        observableDictionaryQuery.DictionaryChanged += ObservableDictionaryQueryDictionaryChanged;
        Evaluate();
    }

    public override string ToString() =>
        $"count of {observableDictionaryQuery}";
}
