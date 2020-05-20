using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Player : ReactiveObject {
        public string Name { get; }
        public Session Session { get; }

        private int _connections;

        public Player(string name, Session session) {
            Requires.NotNullOrWhiteSpace(name, nameof(name));

            Name = name;
            Session = session;


        }

        [Reactive]
        public int Team { get; set; } = Models.Team.Spectator;

        [Reactive]
        public bool IsGameMaster { get; set; }

        public bool IsConnected => _connections > 0;

        public void LeaveSession() {
            Session.Kick(Name);
        }

        public Interaction<Unit, string[]> GetWordsInteraction { get; } = new Interaction<Unit, string[]>();

        public Subject<bool> YesNoSignal { get; } = new Subject<bool>();

        public IDisposable GetConnectionToken() {
            Interlocked.Increment(ref _connections);
            return Disposable.Create(() => Interlocked.Decrement(ref _connections));
        }
    }
}
