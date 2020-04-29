using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Round : ReactiveObject {
        private readonly Session _session;

        public Round(Session session) {
            Requires.NotNull(session, nameof(session));

            _session = session;
        }

        public async Task Run(CancellationToken token) {
            var roundCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            var players = _session.PlayersOrdered;
            var words = _session.SourceWords.ToList();

            int playerIndex = 0;
            try {
                while (words.Count > 0) {
                    var player = players[playerIndex++ % players.Count];

                    using var run = new Run(player, words);
                    CurrentRun = run;

                    await run.Start(roundCancellationTokenSource.Token);

                    var team = _session.LookupTeam(player.Team);

                    team.Score.HitCount += run.Score.HitCount;
                    team.Score.MissCount += run.Score.MissCount;
                }
            } finally {
                CurrentRun = null;
            }
        }

        [Reactive]
        public Run CurrentRun { get; private set; }
    }
}
