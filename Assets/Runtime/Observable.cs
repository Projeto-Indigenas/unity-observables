using Observables.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Observables
{
    public class Observable<TObserved>
    {
        private static readonly Predicate<WeakReference<Action<TObserved>>> _searchActionPredicate = each => each.TryGetTarget(out Action<TObserved> target) && target.Equals(_searchingAction);
        private static Action<TObserved> _searchingAction = default;

        private static long _currentId = long.MinValue;

        private readonly List<long> _keys = new List<long>();
        private readonly ConditionalWeakTable<object, IdHolder> _keysIds = new ConditionalWeakTable<object, IdHolder>();
        private readonly Dictionary<long, List<WeakReference<Action<TObserved>>>> _observers = new Dictionary<long, List<WeakReference<Action<TObserved>>>>();
        private readonly Dictionary<long, List<IObserverInfo>> _observersWithPayload = new Dictionary<long, List<IObserverInfo>>();

        public void Observe(object observer, Action<TObserved> action)
        {
            long key = GetId(observer, out bool shouldAdd);

            if (_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list))
            {
                if (Contains(list, action)) return;

                list.Add(new WeakReference<Action<TObserved>>(action));

                return;
            }

            WeakReference<Action<TObserved>> weakAction = new WeakReference<Action<TObserved>>(action);
            List<WeakReference<Action<TObserved>>> newList = new List<WeakReference<Action<TObserved>>> { weakAction };
            _observers.Add(key, newList);

            if (shouldAdd) _keys.Add(key);

            SetupDestructor(observer);
        }

        public void Observe<TPayload>(object observer, Action<TObserved, TPayload> action)
        {
            long key = GetId(observer, out bool shouldAdd);

            if (_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list))
            {
                if (Contains(list, action)) return;

                list.Add(new ObserverPayloadInfo<TObserved, TPayload>(action));

                return;
            }

            IObserverInfo observerInfo = new ObserverPayloadInfo<TObserved, TPayload>(action);
            List<IObserverInfo> newList = new List<IObserverInfo> { observerInfo };
            _observersWithPayload.Add(key, newList);

            if (shouldAdd) _keys.Add(key);

            SetupDestructor(observer);
        }

        public void RemoveObserver(object observer, Action<TObserved> action)
        {
            long key = GetId(observer, out _);

            if (!_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list)) return;

            _searchingAction = action;
            list.RemoveWhere(_searchActionPredicate);
            _searchingAction = null;
        }

        public void RemoveObserver<TPayload>(object observer, Action<TObserved, TPayload> action)
        {
            long key = GetId(observer, out _);

            if (!_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) return;

            for (int index = 0; index < list.Count; index++)
            {
                IObserverInfo current = list[index];

                if (!(current is ObserverPayloadInfo<TObserved, TPayload> observerPayloadInfo)) continue;

                observerPayloadInfo.Unregister(action);
            }
        }

        public void ClearObservers()
        {
            _keys.Clear();
            _observers.Clear();
            _observersWithPayload.Clear();
        }

        public static void InvokeMessage(Observable<TObserved> observable, TObserved observed)
        {
#if OBSERVABLES_DEVELOPMENT
            try
            {
#endif
                observable.NotifyObservers(observed);
#if OBSERVABLES_DEVELOPMENT
            }
            catch (Exception ex)
            {
                Logger.Log($"Unexpected exception when invoking message. \n" +
                    $"observed -> {observed}\n" +
                    $"exception -> {ex}");
            }
#endif
        }

        public static void InvokeMessage<TPayload>(Observable<TObserved> observable, TObserved observed, TPayload payload)
        {
#if OBSERVABLES_DEVELOPMENT
            try
            {
#endif
                observable.NotifyObservers(observed, payload);
#if OBSERVABLES_DEVELOPMENT
            }
            catch (Exception ex)
            {
                Logger.Log($"Unexpected exception when invoking message. \n" +
                    $"observed -> {observed}\n" +
                    $"payload -> {payload}\n" +
                    $"exception -> {ex}");
            }
#endif
        }

        private void SetupDestructor(object observer)
        {
            if (observer is ADestructorObserver destructorObserver)
            {
                destructorObserver.destructorObservable.Observe(this, OnObserverDestructed);

                return;
            }

#if UNITY_2019_1_OR_NEWER
            if (observer is AObservableBehaviour observableBehaviour)
            {
                observableBehaviour.onDestroyObservable.Observe(this, OnObserverDestroyed);

                return;
            }
#endif

#if OBSERVABLES_DEVELOPMENT
            Type observerType = observer.GetType();
            bool isObservable = observerType.IsGenericType && observerType.GetGenericTypeDefinition() == typeof(Observable<>);
            if (isObservable) return;

            Logger.Log($"Observer is not either DestructorObservable nor ObservableBehaviour. " +
                $"It should inherit from one of them or stop observing manually.\n" +
                $"Observable type: {observer}");
#endif
        }

        private bool Contains(List<WeakReference<Action<TObserved>>> list, Action<TObserved> action)
        {
            for (int index = list.Count - 1; index >= 0; index--)
            {
                WeakReference<Action<TObserved>> current = list[index];

                if (!current.TryGetTarget(out Action<TObserved> target))
                {
                    list.RemoveAt(index);

                    continue;
                }

                if (target.Equals(action)) return true;
            }

            return false;
        }

        private bool Contains<TPayload>(List<IObserverInfo> list, Action<TObserved, TPayload> action)
        {
            for (int index = 0; index < list.Count; index++)
            {
                IObserverInfo current = list[index];
                if (current is ObserverPayloadInfo<TObserved, TPayload> observerPayloadInfo)
                {
                    return observerPayloadInfo.IsRegistered(action);
                }
            }
            return false;
        }

        private void NotifyObservers(TObserved observed)
        {
            for (int index = 0; index < _keys.Count; index++)
            {
                long key = _keys[index];

                if (!_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list)) continue;

                for (int observerIndex = list.Count - 1; observerIndex >= 0; observerIndex--)
                {
                    WeakReference<Action<TObserved>> current = list[observerIndex];

                    if (!current.TryGetTarget(out Action<TObserved> target))
                    {
                        list.RemoveAt(observerIndex);

                        continue;
                    }

                    target?.Invoke(observed);
                }
            }
        }

        private void NotifyObservers<TPayload>(TObserved observed, TPayload payload)
        {
            for (int index = 0; index < _keys.Count; index++)
            {
                long key = _keys[index];

                if (!_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) continue;

                for (int observerIndex = 0; observerIndex < list.Count; observerIndex++)
                {
                    IObserverInfo observerInfo = list[observerIndex];

                    if (!(observerInfo is ObserverPayloadInfo<TObserved, TPayload> observerPayloadInfo)) continue;

                    observerPayloadInfo?.Invoke(observed, payload);
                }
            }
        }

        private void OnObserverDestructed(ADestructorObserver observer)
        {
            observer.destructorObservable.RemoveObserver(this, OnObserverDestructed);

            long key = GetId(observer, out _);
            _keys.Remove(key);
            _observers.Remove(key);
            _observersWithPayload.Remove(key);
        }

#if UNITY_INCLUDE_TESTS
        private void OnObserverDestroyed(AObservableBehaviour observer)
        {
            observer.onDestroyObservable.RemoveObserver(this, OnObserverDestroyed);

            long key = GetId(observer, out _);
            _keys.Remove(key);
            _observers.Remove(key);
            _observersWithPayload.Remove(key);
        }
#endif

        private long GetId(object observer, out bool firstTime)
        {
            if (_keysIds.TryGetValue(observer, out IdHolder holder))
            {
                firstTime = false;
                return holder.id;
            }

            _keysIds.Add(observer, holder = new IdHolder(_currentId++));
            firstTime = true;
            return holder.id;
        }

        private class IdHolder
        {
            public readonly long id;

            public IdHolder(long id) => this.id = id;
        }
    }
}
