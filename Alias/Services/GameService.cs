using System;
using System.Reactive.Linq;
using Alias.Models;
using DynamicData;
using DynamicData.Kernel;
using DynamicData.Aggregation;
using DynamicData.Binding;

namespace Alias.Services {
    public class GameService {
        private readonly SourceCache<Session, string> _sessions = new SourceCache<Session, string>(x => x.Id);

        public GameService() {
            _sessions
                .Connect()
                .SubscribeMany(x => x.Players
                    .ToObservableChangeSet()
                    .Count()
                    .Do(count => {
                        if (count == 0)
                            _sessions.Remove(x);
                    })
                    .Subscribe()
                )
                .DisposeMany()
                .Subscribe();
        }

        public Session GetSession(string id) {
            lock (_sessions) {
                var session = _sessions.Lookup(id).ValueOrDefault();
                if (session == null)
                    _sessions.AddOrUpdate(session = new Session(id));

                return session;
            }
        }
    }
}
