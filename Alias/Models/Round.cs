using System.Linq;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Round : ReactiveObject {
        private readonly Session _session;

        public int Index { get; }

        public Round(Session session, int index) {
            Requires.NotNull(session, nameof(session));

            _session = session;

            Index = index;
        }

        public async Task<bool> Run(CancellationToken token) {
            var roundCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            var start = await _session.GameMaster.YesNoInteraction.Handle(Unit.Default).ToTask(roundCancellationTokenSource.Token);
            if (!start)
                return false;

            var players = _session.PlayersOrdered;
            var words = _session.SourceWords.ToList();

            int playerIndex = _session.NextPlayerIndex;
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
                _session.NextPlayerIndex = playerIndex;
            }

            return true;
        }

        [Reactive]
        public Run CurrentRun { get; private set; }
    }
}
