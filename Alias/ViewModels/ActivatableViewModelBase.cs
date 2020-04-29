using System.Threading;
using ReactiveUI;

namespace Alias.ViewModels {
    public abstract class ActivatableViewModelBase : ReactiveObject, IActivatableViewModel {
        ViewModelActivator IActivatableViewModel.Activator { get; } = new ViewModelActivator();

        internal static int s_Count;
        internal int m_Index;
     
        public ActivatableViewModelBase() {
            m_Index = Interlocked.Increment(ref s_Count);
        }
    }
}
