using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Alias.Models {
    public class Score : ReactiveObject {
        [Reactive]
        public int HitCount { get; set; }
        [Reactive]
        public int MissCount { get; set; }
    }
}