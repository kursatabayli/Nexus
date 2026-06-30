namespace Nexus.Service.Extensions;

internal static class ObservableExtensions
{
    public static Task<T?> FirstOrDefaultAsync<T>(this IObservable<T> observable)
    {
        var tcs = new TaskCompletionSource<T?>();
        IDisposable? subscription = null;

        var observer = new ActionObserver<T>(
            onNext: value =>
            {
                tcs.TrySetResult(value);
                subscription?.Dispose();
            },
            onError: ex => tcs.TrySetException(ex),
            onCompleted: () => tcs.TrySetResult(default)
        );

        subscription = observable.Subscribe(observer);

        return tcs.Task;
    }

    private sealed class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public ActionObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void OnNext(T value) => _onNext(value);
        public void OnError(Exception error) => _onError(error);
        public void OnCompleted() => _onCompleted();
    }
}