using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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

        private CancellationTokenSource _gameCancellationTokenSource;

        public string Id { get; }

        public ReadOnlyObservableCollection<Player> Players { get; }
        public ReadOnlyObservableCollection<Team> Teams { get; }

        private int _isRunning;

        public DateTimeOffset LastRunTime { get; private set; } = DateTimeOffset.Now;

        public Session(string id) {
            Requires.NotNullOrWhiteSpace(id, nameof(id));

            Id = id;

            Debug.WriteLine($"{nameof(Session)} #{Id}: Created");

            _players.Connect()
                .Count()
                .Select(x => x / 2)
                .BindTo(this, x => x.MaximumTeamCount);

            static ReadOnlyObservableCollection<TObject> createView<TObject, TKey>(IConnectableCache<TObject, TKey> source) {
                source.Connect()
                    .Bind(out var view)
                    .Subscribe();
                return view;
            }

            Players = createView(_players); ;
            Teams = createView(_teams); ;
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

                if (_isRunning == 1 && !Team.IsSpectator(existing.Value))
                    Cancel();
            }
        }

        public bool CanRun() {
            if (_isRunning != 0)
                return false;

            var players = _players.Items
                .ToList();

            if (players.Any(x => !x.IsConnected))
                return false;

            if (players.Count(x => x.IsGameMaster) != 1)
                return false;

            var teams = players
                .GroupBy(x => x.Team)
                .ToList();
            var activeTeams = teams
                .Where(x => x.Key >= 0)
                .ToList();

#if DEBUG
            return true;
#endif
            return activeTeams.Count > 1 && activeTeams.All(x => x.Count() > 1);
        }

        public async Task Run(CancellationToken token = default) {
            if (!CanRun())
                return;

            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
                return;
            using var _ = Disposable.Create(() => Interlocked.Exchange(ref _isRunning, 0));

            LastRunTime = DateTimeOffset.Now;

            CancellationTokenSource cancellationTokenSource = null;
            try {
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                _gameCancellationTokenSource = cancellationTokenSource;

                var allPlayers = _players.Items
                    .ToList();

                var gameMaster = allPlayers
                    .Single(x => x.IsGameMaster);

                var activePlayers = allPlayers
                    .Where(Team.IsActive)
                    .ToList();

                var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var client in activePlayers) {
                    var playerWords = await client.GetWordsInteraction
                        .Handle(Unit.Default)
                        .FirstOrDefaultAsync()
                        .ToTask(_gameCancellationTokenSource.Token);

                    words.UnionWith(playerWords.Take(MaximumWordCount));
                }
                words.RemoveWhere(string.IsNullOrWhiteSpace);

                if (words.Count == 0)
                    return;

                _teams.Edit(mutable => {
                    mutable.Clear();
                    mutable.AddOrUpdate(
                        activePlayers
                            .Select(x => x.Team)
                            .Distinct()
                            .Select(x => new Team(x))
                    );
                });

                var teams = _teams.Items.ToList();

                var playerBuffer = activePlayers.ToList();
                var playersOrdered = new List<Player>();

                var teamIndex = 0;
                for (int i = 0; i < activePlayers.Count; i++) {
                    Player player = null;
                    while (player == null) {
                        var team = teams[teamIndex++ % teams.Count];
                        player = playerBuffer.FirstOrDefault(x => x.Team == team.Id);
                    }

                    playersOrdered.Add(player);
                    playerBuffer.Remove(player);
                }

                GameMaster = gameMaster;
                PlayersOrdered = playersOrdered;
                SourceWords = words.ToList();

                for (int roundIndex = 0; !cancellationTokenSource.IsCancellationRequested; roundIndex++) {
                    CurrentRound = new Round(this, roundIndex);

                    if (!await CurrentRound.Run(cancellationTokenSource.Token))
                        break;
                }

            } finally {
                cancellationTokenSource?.Cancel();

                PlayersOrdered = null;
                GameMaster = null;
                SourceWords = null;

                NextPlayerIndex = 0;

                CurrentRound = null;
            }
        }

        // "registry"
        public Player GameMaster { get; private set; }
        public IReadOnlyList<Player> PlayersOrdered { get; private set; }
        public IReadOnlyList<string> SourceWords { get; private set; }
        public int NextPlayerIndex { get; set; }
        public Team LookupTeam(int id) => _teams.Lookup(id).ValueOrDefault();

        [Reactive]
        public int MaximumTeamCount { get; private set; }

        [Reactive]
        public byte MaximumWordCount { get; set; } = 5;
        
        [Reactive]
        public Round CurrentRound { get; private set; }

        public void Cancel() {
            _gameCancellationTokenSource?.Dispose();
            _gameCancellationTokenSource = null;
        }

        public void Dispose() {
            Debug.WriteLine($"{nameof(Session)} #{Id}: Disposing");

            Cancel();

            _players?.Dispose();
            _teams?.Dispose();
        }
    }
}
