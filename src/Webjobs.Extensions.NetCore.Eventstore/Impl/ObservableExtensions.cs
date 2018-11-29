using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext)
        {
            return source.Select(e => Observable.Defer(() => onNext(e).ToObservable())).Concat()
                .Subscribe(
                    e => { });
        }
        
        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Action<Exception> onError, Action onCompleted)
        {
            return source.Select(e => Observable.Defer(() => onNext(e).ToObservable())).Concat()
                .Subscribe(
                    e => { }, // empty
                    onError,
                    onCompleted);
        }
    }
}