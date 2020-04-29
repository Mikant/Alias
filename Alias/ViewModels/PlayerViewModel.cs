using Alias.Models;

namespace Alias.ViewModels {
    public class PlayerViewModel : ItemViewModelBase<Player> {
        public PlayerViewModel(Player content)
            : base(content) {
        }
    }
}
