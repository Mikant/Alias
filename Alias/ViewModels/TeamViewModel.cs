using Alias.Models;

namespace Alias.ViewModels {
    public class TeamViewModel : ItemViewModelBase<Team> {
        public TeamViewModel(Team content)
            : base(content) {
        }
    }
}