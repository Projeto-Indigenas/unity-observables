using System;
using System.Collections.Generic;

#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
#endif

namespace Observables
{
    public class ObserverPayloadInfo<TObserved, TPayload> : IObserverInfo
    {
        private readonly List<Action<TObserved, TPayload>> _observerEvents = new List<Action<TObserved, TPayload>>();

        public ObserverPayloadInfo(Action<TObserved, TPayload> action)
        {
            _observerEvents.Add(action);
        }

        public void Register(Action<TObserved, TPayload> action)
        {
            if (_observerEvents.Contains(action)) return;

            _observerEvents.Add(action);
        }

        public void Unregister(Action<TObserved, TPayload> action)
        {
            _observerEvents.Remove(action);
        }

        public bool IsRegistered(Action<TObserved, TPayload> action)
        {
            return _observerEvents.Contains(action);
        }

        public void Clear()
        {
            _observerEvents.Clear();
        }

        public void Invoke(TObserved observed, TPayload payload)
        {
            for (int index = 0; index < _observerEvents.Count; index++)
            {
                Action<TObserved, TPayload> current = _observerEvents[index];

                current?.Invoke(observed, payload);
            }
        }
    }
}
