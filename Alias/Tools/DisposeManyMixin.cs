using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Alias.Tools {
    public static class DisposeManyMixin {
        public static IObservable<TSource> DisposeMany<TSource>(this IObservable<TSource> source)
            where TSource : IDisposable {
            return new DisposeManyImpl<TSource>(source);
        }

        private class DisposeManyImpl<TSource> : IObservable<TSource>
            where TSource : IDisposable {

            private readonly IObservable<TSource> _source;

            private readonly SerialDisposable _cradle = new SerialDisposable();

            public DisposeManyImpl(IObservable<TSource> source) {
                _source = source.Do(Observer.Create<TSource>(
                    x => _cradle.Disposable = x,
                    e => _cradle.Disposable = null,
                    () => _cradle.Disposable = null
                ));
            }

            public IDisposable Subscribe(IObserver<TSource> observer) {
                return new CompositeDisposable(
                    _source.Subscribe(observer),
                    _cradle
                );
            }
        }
    }
}
