using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Alias.Models;
using Alias.Services;
using Alias.Tools;
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

        public ObservableCollectionExtended<string> Words { get; } = new ObservableCollectionExtended<string>();

        public Subject<bool> YesNoSignal { get; } = new Subject<bool>();

        public IndexViewModel(
            ProtectedLocalStorage localStorageService,
            GameService gameService
        ) {
            _localStorageService = localStorageService;
            _gameService = gameService;

            Debug.WriteLine($"{nameof(IndexViewModel)} #{m_Index} .ctor");

            string[] filterWords() => Words.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            this.WhenActivated(context => {
                Debug.WriteLine($"{nameof(IndexViewModel)} #{m_Index} Activating");

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
                    .Do(x => _localStorageService.SetAsync(nameof(Words), filterWords()))
                    .Subscribe()
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.Player)
                    .Select(x => x != null)
                    .BindTo(this, x => x.IsLoggedIn)
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.Player)
                    .Select(x => x != null ? x.GetConnectionToken() : Disposable.Empty)
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith(context);

                this.WhenAnyValue(x => x.Player)
                    .Where(x => x != null)
                    .Select(x => {
                        return new CompositeDisposable(
                            x.GetWordsInteraction.RegisterHandler(interactionContext =>
                                interactionContext.SetOutput(filterWords())
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

            using (Words.SuspendNotifications()) {
                Words.Clear();

                var words = await GetStoredItemOrResetAsync<string[]>(nameof(Words));
                if (words != null) {
                    Words.AddRange(words);
                }
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

        public void DelegateAdminRights(Player player) {
            if (Player == player || !Player.IsGameMaster)
                return;

            player.IsGameMaster = true;
            Player.IsGameMaster = false;
        }

        public string MaximumWordCountText {
            get => (Player?.Session.MaximumWordCount ?? 0).ToString();
            set {
                if (Player != null && byte.TryParse(value, out var x))
                    Player.Session.MaximumWordCount = x;
            }
        }
    }
}
