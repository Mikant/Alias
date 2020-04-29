using System.ComponentModel;
using System.Reactive.Disposables;
using System.Threading;
using ReactiveUI;
using ReactiveUI.Blazor;

namespace Alias.Views {
    public abstract class ActivatableViewBase<T> : ReactiveComponentBase<T>
        where T : class, INotifyPropertyChanged {
      
        internal static int s_Count;
        internal int m_Index;
        
        public ActivatableViewBase() {
            m_Index = Interlocked.Increment(ref s_Count);

            this.WhenActivated((CompositeDisposable context) => { /* 4VM */ });
        }

        protected override void OnAfterRender(bool isFirstRender) {
            base.OnAfterRender(isFirstRender);
        }
    }
}
