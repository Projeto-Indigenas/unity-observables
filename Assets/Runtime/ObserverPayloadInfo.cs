using System;
using System.Collections.Generic;

namespace Observables
{
    public class ObserverPayloadInfo<TObserved, TPayload> : IObserverInfo
    {
        private readonly List<WeakReference<Action<TObserved, TPayload>>> _observerEvents = new List<WeakReference<Action<TObserved, TPayload>>>();

        public ObserverPayloadInfo(Action<TObserved, TPayload> action)
        {
            _observerEvents.Add(new WeakReference<Action<TObserved, TPayload>>(action));
        }

        public void Register(Action<TObserved, TPayload> action)
        {
            if (IsRegistered(action)) return;

            _observerEvents.Add(new WeakReference<Action<TObserved, TPayload>>(action));
        }

        public void Unregister(Action<TObserved, TPayload> action)
        {
            for (int index = _observerEvents.Count - 1; index >= 0; index--)
            {
                WeakReference<Action<TObserved, TPayload>> current = _observerEvents[index];

                if (!current.TryGetTarget(out Action<TObserved, TPayload> target))
                {
                    _observerEvents.RemoveAt(index);

                    continue;
                }

                if (target.Equals(action))
                {
                    _observerEvents.RemoveAt(index);

                    return;
                }
            }
        }

        public bool IsRegistered(Action<TObserved, TPayload> action)
        {
            for (int index = _observerEvents.Count - 1; index >= 0; index--)
            {
                WeakReference<Action<TObserved, TPayload>> current = _observerEvents[index];

                if (!current.TryGetTarget(out Action<TObserved, TPayload> target))
                {
                    _observerEvents.RemoveAt(index);

                    continue;
                }

                if (target.Equals(action)) return true;
            }

            return false;
        }

        public void Clear()
        {
            _observerEvents.Clear();
        }

        public void Invoke(TObserved observed, TPayload payload)
        {
            for (int index = _observerEvents.Count - 1; index >= 0; index--)
            {
                WeakReference<Action<TObserved, TPayload>> current = _observerEvents[index];

                if (!current.TryGetTarget(out Action<TObserved, TPayload> target)) 
                {
                    _observerEvents.RemoveAt(index);

                    continue; 
                }

                target?.Invoke(observed, payload);
            }
        }
    }
}
