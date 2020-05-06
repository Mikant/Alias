using System.Reactive;
using System.Reactive.Concurrency;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Player : ReactiveObject {
        public string Name { get; }
        public Session Session { get; }

        public Player(string name, Session session) {
            Requires.NotNullOrWhiteSpace(name, nameof(name));

            Name = name;
            Session = session;
        }

        [Reactive]
        public int Team { get; set; }

        [Reactive]
        public bool IsGameMaster { get; set; }

        public void LeaveSession() {
            Session.Kick(Name);
        }

        public Interaction<Unit, string[]> GetWordsInteraction { get; } = new Interaction<Unit, string[]>();

        public Interaction<Unit, bool> YesNoInteraction { get; } = new Interaction<Unit, bool>(TaskPoolScheduler.Default);
    }
}
