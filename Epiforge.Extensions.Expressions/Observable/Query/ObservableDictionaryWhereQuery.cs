namespace Epiforge.Extensions.Expressions.Observable.Query;

sealed class ObservableDictionaryWhereQuery<TKey, TValue> :
    ObservableDictionaryQuery<TKey, TValue>
    where TKey : notnull
{
    public ObservableDictionaryWhereQuery(CollectionObserver collectionObserver, ObservableDictionaryQuery<TKey, TValue> source, Expression<Func<KeyValuePair<TKey, TValue>, bool>> predicate) :
        base(collectionObserver)
    {
        access = new();
        evaluationsChanging = new();
        keyComparer = EqualityComparer<TKey>.Default;
        observableExpressions = new();
        Predicate = predicate;
        result = new();
        this.source = source;
    }

    readonly object access;
    readonly Dictionary<IObservableExpression<KeyValuePair<TKey, TValue>, bool>, (Exception? fault, bool result)> evaluationsChanging;
    readonly IEqualityComparer<TKey> keyComparer;
    readonly Dictionary<TKey, IObservableExpression<KeyValuePair<TKey, TValue>, bool>> observableExpressions;
    readonly ObservableDictionary<TKey, TValue> result;
    readonly ObservableDictionaryQuery<TKey, TValue> source;

    internal readonly Expression<Func<KeyValuePair<TKey, TValue>, bool>> Predicate;

    public override TValue this[TKey key]
    {
        get
        {
            lock (access)
                return result[key];
        }
    }

    public override int Count
    {
        get
        {
            lock (access)
                return result.Count;
        }
    }

    public override IEnumerable<TKey> Keys
    {
        get
        {
            lock (access)
                return result.Keys.ToList().AsReadOnly();
        }
    }

    public override IEnumerable<TValue> Values
    {
        get
        {
            lock (access)
                return result.Values.ToList().AsReadOnly();
        }
    }

    public override bool ContainsKey(TKey key)
    {
        lock (access)
            return result.ContainsKey(key);
    }

    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            var removedFromCache = source.QueryDisposed(this);
            if (removedFromCache)
            {
                foreach (var observableExpression in observableExpressions.Values)
                {
                    observableExpression.PropertyChanging -= ObservableExpressionPropertyChanging;
                    observableExpression.PropertyChanged -= ObservableExpressionPropertyChanged;
                    observableExpression.Dispose();
                }
                source.DictionaryChanged -= SourceDictionaryChanged;
                result.DictionaryChanged -= ResultDictionaryChanged;
                result.PropertyChanging -= ResultPropertyChanging;
                result.PropertyChanged -= ResultPropertyChanged;
                RemovedFromCache();
            }
            return removedFromCache;
        }
        return true;
    }

    public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        lock (access)
            foreach (var keyValuePair in result)
                yield return keyValuePair;
    }

    protected override void OnInitialization()
    {
        var faultList = new FaultList();
        var expressionObserver = collectionObserver.ExpressionObserver;
        foreach (var keyValuePair in source)
        {
            var observableExpression = expressionObserver.ObserveWithoutOptimization(Predicate, keyValuePair);
            if (!faultList.Check(observableExpression) && observableExpression.Evaluation.Result)
                result.Add(keyValuePair.Key, keyValuePair.Value);
            observableExpression.PropertyChanging += ObservableExpressionPropertyChanging;
            observableExpression.PropertyChanged += ObservableExpressionPropertyChanged;
            observableExpressions.Add(keyValuePair.Key, observableExpression);
        }
        OperationFault = faultList.Fault;
        source.DictionaryChanged += SourceDictionaryChanged;
        result.DictionaryChanged += ResultDictionaryChanged;
        result.PropertyChanging += ResultPropertyChanging;
        result.PropertyChanged += ResultPropertyChanged;
    }

    void ResultDictionaryChanged(object? sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        OnDictionaryChanged(e);

    void ResultPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(e);

    void ResultPropertyChanging(object? sender, PropertyChangingEventArgs e) =>
        OnPropertyChanging(e);

    void ObservableExpressionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is IObservableExpression<KeyValuePair<TKey, TValue>, bool> observableExpression && e.PropertyName == nameof(IObservableExpression<KeyValuePair<TKey, TValue>, bool>.Evaluation))
            lock (access)
            {
                var (oldFault, oldPredicateResult) = evaluationsChanging![observableExpression];
                evaluationsChanging.Remove(observableExpression);
                var (newFault, newPredicateResult) = observableExpression.Evaluation;
                var keyValuePair = observableExpression.Argument;
                if (!oldPredicateResult && newPredicateResult)
                    result.Add(keyValuePair.Key, keyValuePair.Value);
                else if (oldPredicateResult && !newPredicateResult)
                    result.Remove(keyValuePair.Key);
                if (FaultList.ExchangeKeyFault(OperationFault, observableExpression.Argument.Key, keyComparer, oldFault, newFault, out var newOperationFault))
                    OperationFault = newOperationFault;
            }
    }

    void ObservableExpressionPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is IObservableExpression<KeyValuePair<TKey, TValue>, bool> observableExpression && e.PropertyName == nameof(IObservableExpression<KeyValuePair<TKey, TValue>, bool>.Evaluation))
            lock (access)
                evaluationsChanging.Add(observableExpression, observableExpression.Evaluation);
    }

    void SourceDictionaryChanged(object? sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
    {
        lock (access)
        {
            var expressionObserver = collectionObserver.ExpressionObserver;
            if (e.Action is NotifyDictionaryChangedAction.Reset)
            {
                foreach (var observableExpression in observableExpressions.Values)
                {
                    observableExpression.PropertyChanging -= ObservableExpressionPropertyChanging;
                    observableExpression.PropertyChanged -= ObservableExpressionPropertyChanged;
                    observableExpression.Dispose();
                }
                observableExpressions.Clear();
                evaluationsChanging.Clear();
                var newResult = new ObservableDictionary<TKey, TValue>();
                var faultList = new FaultList();
                foreach (var keyValuePair in source)
                {
                    var observableExpression = expressionObserver.ObserveWithoutOptimization(Predicate, keyValuePair);
                    if (!faultList.Check(observableExpression) && observableExpression.Evaluation.Result)
                        newResult.Add(keyValuePair.Key, keyValuePair.Value);
                    observableExpression.PropertyChanging += ObservableExpressionPropertyChanging;
                    observableExpression.PropertyChanged += ObservableExpressionPropertyChanged;
                    observableExpressions.Add(keyValuePair.Key, observableExpression);
                }
                result.Reset(newResult);
                OperationFault = faultList.Fault;
            }
            else
            {
                FaultList? faultList = null;
                foreach (var keyValuePair in e.OldItems)
                {
                    var key = keyValuePair.Key;
                    var observableExpression = observableExpressions[key];
                    var (fault, predicateResult) = observableExpression.Evaluation;
                    if (fault is not null)
                    {
                        faultList ??= new FaultList(OperationFault);
                        faultList.RemoveKey(key, keyComparer);
                    }
                    else if (predicateResult)
                        result.Remove(key);
                    observableExpression.PropertyChanging -= ObservableExpressionPropertyChanging;
                    observableExpression.PropertyChanged -= ObservableExpressionPropertyChanged;
                    observableExpression.Dispose();
                    observableExpressions.Remove(key);
                    evaluationsChanging.Remove(observableExpression);
                    result.Remove(key);
                }
                foreach (var keyValuePair in e.NewItems)
                {
                    var key = keyValuePair.Key;
                    var observableExpression = expressionObserver.ObserveWithoutOptimization(Predicate, keyValuePair);
                    var (fault, predicateResult) = observableExpression.Evaluation;
                    if (fault is not null)
                    {
                        faultList ??= new FaultList(OperationFault);
                        faultList.Check(observableExpression);
                    }
                    else if (predicateResult)
                        result.Add(key, keyValuePair.Value);
                    observableExpression.PropertyChanging += ObservableExpressionPropertyChanging;
                    observableExpression.PropertyChanged += ObservableExpressionPropertyChanged;
                    observableExpressions.Add(key, observableExpression);
                }
                if (faultList is not null)
                    OperationFault = faultList.Fault;
            }
        }
    }

    public override string ToString() =>
        $"{source} matching {Predicate}";

    public override bool TryGetValue(TKey key, out TValue value)
    {
        lock (access)
            return result.TryGetValue(key, out value);
    }
}
