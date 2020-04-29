using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Alias.Models;
using Alias.Services;
using Alias.Tools;
using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.ProtectedBrowserStorage;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.ViewModels {
    public class IndexViewModel : ActivatableViewModelBase {
        private readonly ProtectedLocalStorage _localStorageService;
        private readonly GameService _gameService;

        private string _sessionId;

        [Reactive]
        public bool IsInitialized { get; private set; }

        [Reactive]
        public string Username { get; set; }

        [Reactive]
        public Player Player { get; private set; }

        [Reactive]
        public bool IsLoggedIn { get; private set; }

        public ObservableCollectionExtended<string> Words { get; } = new ObservableCollectionExtended<string>(new string[5]);

        public ReadOnlyObservableCollection<PlayerViewModel> Players { get; }
        public ReadOnlyObservableCollection<TeamViewModel> Teams { get; }

        public Subject<bool> YesNoSignal { get; } = new Subject<bool>();

        public IndexViewModel(
            ProtectedLocalStorage localStorageService,
            GameService gameService
        ) {
            _localStorageService = localStorageService;
            _gameService = gameService;

            Debug.WriteLine($"{nameof(IndexViewModel)} #{m_Index} .ctor");

            var playersProxy = new ObservableCollectionExtended<PlayerViewModel>();
            Players = new ReadOnlyObservableCollection<PlayerViewModel>(playersProxy);

            var teamsProxy = new ObservableCollectionExtended<TeamViewModel>();
            Teams = new ReadOnlyObservableCollection<TeamViewModel>(teamsProxy);

            this.WhenActivated(context => {
                Debug.WriteLine($"{nameof(IndexViewModel)} #{m_Index} Activating");

                // leads to spurious attempts to redraw after disposal...
                //Disposable.Create(playersProxy.Clear)
                //    .DisposeWith(context);
                
                // derived collections
                void bindDerivedCollection<TModel, TKey, TViewModel>(
                    Expression<Func<IndexViewModel, IConnectableCache<TModel, TKey>>> accessor,
                    ObservableCollectionExtended<TViewModel> proxyCollection,
                    Func<TModel, TViewModel> transform
                ) {
                    this.WhenAnyValue(x => x.Player, x => x.Player.Session, accessor, (a, b, c) => a == null || b == null ? null : c) // null propagation
                        .Do(_ => proxyCollection.Clear())
                        .Where(x => x != null)
                        .Select(x => x.Connect()
                            .Transform(transform)
                            .Bind(proxyCollection)
                            .Subscribe()
                        )
                        .DisposeMany()
                        .Subscribe()
                        .DisposeWith(context);
                }

                bindDerivedCollection(x => x.Player.Session.Players, playersProxy, x => new PlayerViewModel(x));
                bindDerivedCollection(x => x.Player.Session.Teams, teamsProxy, x => new TeamViewModel(x));

                // local storage
                this.WhenAnyValue(x => x.Username)
                    .Skip(1) // init
                    .Do(x => _localStorageService.SetAsync(nameof(Username), x ?? string.Empty))
                    .Subscribe()
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.IsLoggedIn)
                    .Skip(1) // init
                    .Do(x => _localStorageService.SetAsync(nameof(IsLoggedIn), x))
                    .Subscribe()
                    .DisposeWith(context);

                Words.ObserveCollectionChanges()
                    .Skip(1) // init
                    .Do(x => _localStorageService.SetAsync(nameof(Words), Words.ToArray()))
                    .Subscribe()
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.Player)
                    .Select(x => x != null)
                    .BindTo(this, x => x.IsLoggedIn)
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.Player)
                    .Where(x => x != null)
                    .Select(x => {
                        return new CompositeDisposable(
                            x.GetWordsInteraction.RegisterHandler(interactionContext =>
                                interactionContext.SetOutput(Words.ToArray())
                            ),
                            x.YesNoInteraction.RegisterHandler(async interactionContext => {
                                interactionContext.SetOutput(await YesNoSignal.FirstAsync().PublishLast());
                            })
                        );
                    })
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith(context);

                YesNoSignal.DisposeWith(context);

                Disposable.Create(() => Debug.WriteLine($"{nameof(IndexViewModel)} #{m_Index} Disposing"))
                    .DisposeWith(context);
            });
        }

        public async Task Initialize(string sessionId) {
            Requires.NotNull(sessionId, nameof(sessionId));
            Requires.ValidState(!IsInitialized, nameof(IsInitialized));
            IsInitialized = true;

            _sessionId = sessionId;

            Username = await GetStoredItemOrResetAsync<string>(nameof(Username));

            var words = await GetStoredItemOrResetAsync<string[]>(nameof(Words));
            if (words != null) {
                var count = Math.Min(words.Length, Words.Count);
                using (Words.SuspendNotifications())
                    for (int i = 0; i < count; i++)
                        Words[i] = words[i];
            }

            var wasLoggedIn = await GetStoredItemOrResetAsync<bool>(nameof(IsLoggedIn));

            if (wasLoggedIn)
                Login();
        }

        private async Task<T> GetStoredItemOrResetAsync<T>(string key, T defaultValue = default) {
            try {
                return await _localStorageService.GetAsync<T>(key);
            } catch {
                await _localStorageService.DeleteAsync(key);
                return defaultValue;
            }
        }

        public void Login() {
            if (string.IsNullOrWhiteSpace(Username))
                return; // to avoid abandoned sessions

            Player = _gameService.GetSession(_sessionId)?.Join(Username);
        }

        public void Logout() {
            if (Player != null) {
                Player?.LeaveSession();
                Player = null;
            }
        }

        public void SetTeam(int team) {
            if (Player != null)
                Player.Team = team;
        }
    }
}
