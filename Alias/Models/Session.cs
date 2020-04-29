using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Kernel;
using DynamicData.Aggregation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Session : ReactiveObject, IDisposable {
        private readonly object _syncRoot = new object();

        private readonly SourceCache<Player, string> _players = new SourceCache<Player, string>(x => x.Name);
        private readonly SourceCache<Team, int> _teams = new SourceCache<Team, int>(x => x.Id);

        public string Id { get; }

        private CancellationTokenSource _gameCancellationTokenSource;

        public Session(string id) {
            Requires.NotNullOrWhiteSpace(id, nameof(id));

            Id = id;

            Debug.WriteLine($"{nameof(Session)} #{Id}: Created");

            _players.Connect()
                .Count()
                .Select(x => x / 2)
                .BindTo(this, x => x.MaxTeams);
        }

        public Player Join(string username) {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            lock (_syncRoot) {
                Debug.WriteLine($"{nameof(Session)} #{Id}: User {username} joining");

                var existing = _players.Lookup(username);
                if (existing.HasValue)
                    return existing.Value;

                var newClient = new Player(username, this) {
                    IsGameMaster = _players.Count == 0
                };

                _players.AddOrUpdate(newClient);

                Debug.WriteLine($"{nameof(Session)} #{Id}: Player count is now {_players.Count}");

                return newClient;
            }
        }

        public void Kick(string username) {
            if (string.IsNullOrWhiteSpace(username))
                return;

            lock (_syncRoot) {
                Debug.WriteLine($"{nameof(Session)} #{Id}: User {username} leaving");

                var existing = _players.Lookup(username);
                if (!existing.HasValue)
                    return;

                _players.Remove(username);

                Debug.WriteLine($"{nameof(Session)} #{Id}: Player count is now {_players.Count}");

                if (existing.Value.IsGameMaster && _players.Count > 0)
                    _players.Items.First().IsGameMaster = true;
            }
        }

        public bool CanRun() {
            lock (_syncRoot) {
                if (IsRunning)
                    return false;

                var players = _players.Items.ToList();
                if (players.Count(x => x.IsGameMaster) != 1)
                    return false;

                var teams = players
                    .GroupBy(x => x.Team)
                    .ToList();
                var activeTeams = teams
                    .Where(x => x.Key >= 0)
                    .ToList();
#if DEBUG
#warning DEBUG
                return activeTeams.Count() > 0 && activeTeams.All(x => x.Count() > 0);
#else
                return activeTeams.Count() > 1 && activeTeams.All(x => x.Count() > 1);
#endif
            }
        }

        public async Task Run(CancellationToken token) {
            CancellationTokenSource cancellationTokenSource = null;
            try {
                lock (_syncRoot) {
                    if (!CanRun())
                        return;
                    IsRunning = true;
                }
                
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                _gameCancellationTokenSource = cancellationTokenSource;

                var playersUnordered = _players.Items.ToList();

                var words = new List<string>();
                foreach (var client in playersUnordered)
                    words.AddRange(await client.GetWordsInteraction.Handle(Unit.Default).FirstOrDefaultAsync().ToTask(_gameCancellationTokenSource.Token));
                words.RemoveAll(string.IsNullOrWhiteSpace);

                if (words.Count == 0)
                    return;

                SourceWords = words;

                _teams.Edit(mutable => {
                    mutable.Clear();
                    mutable.AddOrUpdate(
                        _players.Items
                            .Select(x => x.Team)
                            .Distinct()
                            .Select(x => new Team(x))
                    );
                });

                var teams = _teams.Items.ToList();

                var playerBuffer = playersUnordered.ToList();
                var playersOrdered = new List<Player>();

                var teamIndex = 0;
                for (int i = 0; i < playersUnordered.Count; i++) {
                    Player player = null;
                    while (player == null) {
                        var team = teams[teamIndex++ % teams.Count];
                        player = playerBuffer.FirstOrDefault(x => x.Team == team.Id);
                    }

                    playersOrdered.Add(player);
                    playerBuffer.Remove(player);
                }

                GameMaster = playersUnordered.Single(x => x.IsGameMaster);

                CurrentRoundIndex = 0;
                while (!cancellationTokenSource.IsCancellationRequested) {
                    var start = await GameMaster.YesNoInteraction.Handle(Unit.Default).ToTask(cancellationTokenSource.Token);
                    if (!start)
                        break;

                    CurrentRoundIndex++;

                    var round = new Round(this);
                    CurrentRound = round;

                    await round.Run(cancellationTokenSource.Token);
                }

            } finally {
                cancellationTokenSource?.Cancel();

                PlayersOrdered = null;
                GameMaster = null;
                SourceWords = null;

                IsRunning = false;

                CurrentRound = null;
                CurrentRoundIndex = 0;
            }
        }

        [Reactive]
        public IReadOnlyList<Player> PlayersOrdered { get; private set; }
        [Reactive]
        public Player GameMaster { get; private set; }
        [Reactive]
        public IReadOnlyList<string> SourceWords { get; private set; }

        [Reactive]
        public int MaxTeams { get; private set; }

        [Reactive]
        public int CurrentRoundIndex { get; private set; }
        [Reactive]
        public Round CurrentRound { get; private set; }

        public IConnectableCache<Player, string> Players => _players;
        public IConnectableCache<Team, int> Teams => _teams;

        public Team LookupTeam(int id) => _teams.Lookup(id).ValueOrDefault();

        [Reactive]
        public bool IsRunning { get; private set; }

        public void Dispose() {
            Debug.WriteLine($"{nameof(Session)} #{Id}: Disposing");

            _gameCancellationTokenSource?.Dispose();
            _players?.Dispose();
            _teams?.Dispose();
        }
    }
}
