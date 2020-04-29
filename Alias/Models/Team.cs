using ReactiveUI;

namespace Alias.Models {
    public class Team : ReactiveObject {
        public int Id { get; }

        public Team(int id) {
            Id = id;
        }

        public Score Score { get; } = new Score();
    }
}
