using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Alias.Tools {
    public static class AsUnitMixin {
        public static IObservable<Unit> AsUnit<T>(this IObservable<T> source) => source.Select(_ => Unit.Default);
    }
}