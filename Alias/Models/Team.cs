using ReactiveUI;

namespace Alias.Models {
    public class Team : ReactiveObject {
        public int Id { get; }

        public Team(int id) {
            Id = id;
        }

        public Score Score { get; } = new Score();

        public const int Spectator = -1;

        public static bool IsActive(Player player) => player.Team >= 0;
        public static bool IsSpectator(Player player) => !IsActive(player);
    }
}
