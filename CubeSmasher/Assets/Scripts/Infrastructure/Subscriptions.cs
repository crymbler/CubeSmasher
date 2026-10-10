using System;
using System.Collections.Generic;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Список подписок на события. Каждая подписка добавляется парой (подписать, отписать),
    /// Dispose снимает их в обратном порядке. Так инициализаторы не теряют отписку.
    /// </summary>
    public sealed class Subscriptions : IDisposable
    {
        private readonly List<Action> _undo = new List<Action>();

        public void Track(Action subscribe, Action unsubscribe)
        {
            subscribe();
            _undo.Add(unsubscribe);
        }

        public void Dispose()
        {
            for (int i = _undo.Count - 1; i >= 0; i--)
            {
                _undo[i]();
            }
            _undo.Clear();
        }
    }
}
