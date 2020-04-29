using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Alias.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using Alias.Models;
using Alias.Tools;
using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using ReactiveUI;

namespace Alias.Views {
    public partial class Index : ActivatableViewBase<IndexViewModel> {

        [Parameter]
        public string SessionId { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IndexViewModel IndexViewModel {
            get => ViewModel;
            set => ViewModel = value;
        }

        public Index() {
            Debug.WriteLine($"{nameof(Index)} #{m_Index} .ctor");

            this.WhenActivated(context => {
                Debug.WriteLine($"{nameof(Index)} #{m_Index} Activating");

                static IObservable<Unit> subscribeDerivedCollection<T, U>(ReadOnlyObservableCollection<T> collection)
                    where T : ItemViewModelBase<U>
                    where U : INotifyPropertyChanged =>
                    Observable.Merge(
                        collection.ObserveCollectionChanges().AsUnit(),
                        collection.ToObservableChangeSet().WhenAnyPropertyChanged().AsUnit(),
                        collection.ToObservableChangeSet().MergeMany(v => v.Content.WhenAnyPropertyChanged()).AsUnit()
                    );

                Observable.Merge(
                    subscribeDerivedCollection<PlayerViewModel, Player>(ViewModel.Players),
                    subscribeDerivedCollection<TeamViewModel, Team>(ViewModel.Teams),
                    ViewModel.WhenAnyValue(x => x.Player)
                        .Select(x => x == null ? Observable.Return(Unit.Default) :
                            Observable.Merge(
                                x.WhenAnyValue(v => v.IsGameMaster).AsUnit(),
                                x.WhenAnyValue(v => v.Session)
                                    .Select(v =>
                                        Observable.Merge(
                                            v.WhenAnyValue(i => i.MaxTeams).AsUnit(),
                                            v.WhenAnyValue(i => i.IsRunning).AsUnit()
                                        )
                                    ).Switch()
                            )
                        ).Switch()
                    )
                    //.Throttle(TimeSpan.FromSeconds(0.1), Scheduler.CurrentThread) // this is handled by default
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
#if !DEBUG
                    var r = new Random();
                    var sb = new StringBuilder();
                    for (int i = 0; i < 5; i++)
                        sb.Append((char)r.Next('a', 'z' + 1));

                    SessionId = sb.ToString();
#else
                    SessionId = "debug";
#endif

                    NavigationManager.NavigateTo($"/{SessionId}");
                }

                await ViewModel.Initialize(SessionId);
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
