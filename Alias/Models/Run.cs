using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Validation;

namespace Alias.Models {
    public class Run : ReactiveObject, IDisposable {
        private static readonly Random Random = new Random();

        public static readonly TimeSpan RoundTime = TimeSpan.FromMinutes(1);
        private DateTimeOffset _startTime;

        private readonly List<string> _words;

        private CancellationTokenSource _cancellationTokenSource;

        public Player Player { get; }

        public Run(Player player, List<string> words) {
            Requires.NotNull(player, nameof(player));
            Requires.NotNull(words, nameof(words));

            Player = player;

            _words = words;
        }

        public void Dispose() {
            _cancellationTokenSource?.Dispose();
        }

        public async Task Start(CancellationToken cancellationToken) {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await Player.YesNoInteraction.Handle(Unit.Default).ToTask(_cancellationTokenSource.Token);

            IsRunning = true;
            _startTime = DateTimeOffset.Now;
            _cancellationTokenSource.CancelAfter(RoundTime);

            try {
                while (!_cancellationTokenSource.IsCancellationRequested && _words.Count > 0) {
                    Word = _words[Random.Next(0, _words.Count)];

                    var accept = await Player.YesNoInteraction.Handle(Unit.Default).ToTask(_cancellationTokenSource.Token);
                    if (accept) {
                        _words.Remove(Word);

                        Score.HitCount++;
                    } else {
                        Score.MissCount++;
                    }
                }
            } finally {
                IsRunning = false;
            }
        }

        [Reactive]
        public bool IsRunning { get; set; }

        public TimeSpan TimeRemaining {
            get {
                if (!IsRunning)
                    return RoundTime;

                var remaining = DateTimeOffset.Now - _startTime;
                if (remaining.Ticks <= 0) {
                    remaining = TimeSpan.Zero;
                }

                return remaining;
            }
        }
        
        [Reactive]
        public string Word { get; set; }

        public Score Score { get; } = new Score();
    }
}
