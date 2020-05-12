using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Alias.Tools;
using Alias.ViewModels;
using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ReactiveUI;

namespace Alias.Views {
    public partial class Index : ActivatableViewBase<IndexViewModel> {

        [Parameter]
        public string SessionId { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public IndexViewModel IndexViewModel {
            get => ViewModel;
            set => ViewModel = value;
        }

        public Index() {
            Debug.WriteLine($"{nameof(Index)} #{m_Index} .ctor");

            this.WhenActivated(context => {
                Debug.WriteLine($"{nameof(Index)} #{m_Index} Activating");

                static IObservable<Unit> subscribeToCollectionChanges<T>(ReadOnlyObservableCollection<T> collection)
                    where T : INotifyPropertyChanged =>
                    Observable.Merge(
                        collection.ObserveCollectionChanges().AsUnit(),
                        collection.ToObservableChangeSet().WhenAnyPropertyChanged().AsUnit()
                    );

                Observable.Merge(
                    ViewModel.WhenAnyValue(c => c.Player)
                        .Select(c => c == null ? Observable.Return(Unit.Default) :
                            Observable.Merge(
                                subscribeToCollectionChanges(c.Session.Players),
                                subscribeToCollectionChanges(c.Session.Teams),
                                c.WhenAnyValue(o => o.IsGameMaster).AsUnit(),
                                c.WhenAnyValue(o => o.Session).Select(o => Observable.Merge(
                                    o.WhenAnyValue(v => v.MaximumTeamCount).AsUnit(),
                                    o.WhenAnyValue(v => v.MaximumWordCount).AsUnit(),
                                    o.WhenAnyValue(v => v.CurrentRound)
                                        .Select(v => v == null ? Observable.Return(Unit.Default) : Observable.Merge(
                                            v.WhenAnyValue(i => i.CurrentRun)
                                                .Select(i => i == null ? Observable.Return(Unit.Default) : Observable.Merge(
                                                    i.WhenAnyValue(d => d.Score.HitCount).AsUnit(),
                                                    i.WhenAnyValue(d => d.Score.MissCount).AsUnit(),
                                                    i.WhenAnyValue(d => d.Word).AsUnit()
                                                )).Switch()
                                        )).Switch()
                                )).Switch()
                        )).Switch()
                    )
                    //.Throttle(TimeSpan.FromSeconds(0.1), Scheduler.CurrentThread) // this is handled by default
                    .Do(_ => InvokeAsync(StateHasChanged))
                    .Subscribe()
                    .DisposeWith(context);

                // a timer for the timer
                ViewModel.WhenAnyValue(c => c.Player.Session.CurrentRound.CurrentRun.IsRunning)
                    .Select(x => !x ? Observable.Return(Unit.Default) : Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)).AsUnit())
                    .Switch()
                    .Do(_ => InvokeAsync(StateHasChanged))
                    .Subscribe()
                    .DisposeWith(context);

                Disposable.Create(() => Debug.WriteLine($"{nameof(Index)} #{m_Index} Disposing"))
                    .DisposeWith(context);
            });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            Debug.WriteLine($"{nameof(Index)} #{m_Index} AfterRenderAsync({firstRender})");

            if (firstRender) {
                if (string.IsNullOrWhiteSpace(SessionId)) {
                    var r = new Random();
                    var sb = new StringBuilder();
                    for (int i = 0; i < 5; i++)
                        sb.Append((char)r.Next('a', 'z' + 1));

                    SessionId = sb.ToString();
#if DEBUG
                    SessionId = "debug";
#endif
                    NavigationManager.NavigateTo($"/{SessionId}");
                }

                await ViewModel.Initialize(SessionId);
            } else {

                // hack. blazor rejects to reapply it
                if (ViewModel.Player?.Session.CurrentRound == null)
                    await JSRuntime.InvokeVoidAsync("reload_github_buttons");

            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private static string GetBootstrapStyleForTeam(int team) =>
            team switch
            {
                0 => "primary",
                1 => "success",
                2 => "danger",
                3 => "warning",
                4 => "info",
                _ => "light"
            };
    }
}
