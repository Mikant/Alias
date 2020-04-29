using ReactiveUI;

namespace Alias.ViewModels {
    public abstract class ItemViewModelBase<T> : ReactiveObject {
        protected ItemViewModelBase(T content) {
            Content = content;
        }

        public T Content { get; }
    }
}
