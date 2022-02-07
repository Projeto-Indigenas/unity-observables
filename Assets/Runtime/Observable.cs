using Observables.Destructors;
using Observables.Extensions;
using Observables.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Observables
{
    public class Observable<TObserved>
    {
        private static readonly Predicate<WeakReference<Action<TObserved>>> _searchActionPredicate = each =>
        {
            return each.TryGetTarget(out Action<TObserved> target) && target.Equals(_searchingAction);
        };

        private static Action<TObserved> _searchingAction = default;

        private static long _currentId = long.MinValue;
        private readonly ConditionalWeakTable<object, IdHolder> _keysIds = new ConditionalWeakTable<object, IdHolder>();
        private readonly Dictionary<long, List<WeakReference<Action<TObserved>>>> _observers = new Dictionary<long, List<WeakReference<Action<TObserved>>>>();
        private readonly Dictionary<Type, IObservable> _observablesWithPayload = new Dictionary<Type, IObservable>();

        internal List<long> keys { get; } = new List<long>();

        public static Observable<TObserved> operator +(Observable<TObserved> observable, Action<TObserved> action)
        {
            observable.Observe(action);
            return observable;
        }

        public static Observable<TObserved> operator -(Observable<TObserved> observable, Action<TObserved> action)
        {
            observable.RemoveObserver(action);
            return observable;
        }

        public Observable<TObserved, TPayload> With<TPayload>()
        {
            if (_observablesWithPayload.TryGetValue(typeof(TPayload), out IObservable observable))
            {
                return (Observable<TObserved, TPayload>)observable;
            }

            Observable<TObserved, TPayload> newObservable = new Observable<TObserved, TPayload>(this);
            _observablesWithPayload[typeof(TPayload)] = newObservable;
            return newObservable;
        }

        public void Observe(Action<TObserved> action, bool willBeUnregisteredManually = false)
        {
            object observer = action.Target;

            Observe(observer, action, willBeUnregisteredManually);
        }

        public void Observe(object observer, Action<TObserved> action, bool willBeUnregisteredManually = false)
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

            if (shouldAdd) keys.Add(key);

            SetupDestructor(observer, willBeUnregisteredManually);
        }

        public void RemoveObserver(Action<TObserved> action)
        {
            object observer = action.Target;
            RemoveObserver(observer, action);
        }

        public void RemoveObserver(object observer, Action<TObserved> action)
        {
            long key = GetId(observer, out _);

            if (!_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list)) return;

            _searchingAction = action;
            _ = list.RemoveWhere(_searchActionPredicate);
            _searchingAction = null;
        }

        public void ClearObservers()
        {
            keys.Clear();
            _observers.Clear();

            foreach (IObservable each in _observablesWithPayload.Values)
            {
                each.ClearObservers();
            }
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

        internal void SetupDestructor(object observer, bool
#if OBSERVABLES_DEVELOPMENT
            willBeUnregisteredManually
#else
            _
#endif
            )
        {
            if (observer is ADestructorObserver destructorObserver)
            {
                destructorObserver.destructorObservable.Observe(OnObserverDestructed);

                return;
            }

#if UNITY_2019_1_OR_NEWER
            if (observer is ADestroyableObserver observableBehaviour)
            {
                observableBehaviour.onDestroyObservable.Observe(OnObserverDestroyed);

                return;
            }
#endif

#if OBSERVABLES_DEVELOPMENT
            Type observerType = observer.GetType();
            bool isObservable = observerType.IsGenericType && observerType.GetGenericTypeDefinition() == typeof(Observable<>);
            if (isObservable || willBeUnregisteredManually) return;

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

        private void NotifyObservers(TObserved observed)
        {
            for (int index = 0; index < keys.Count; index++)
            {
                long key = keys[index];

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

        private void OnObserverDestructed(ADestructorObserver observer)
        {
            observer.destructorObservable.RemoveObserver(OnObserverDestructed);

            long key = GetId(observer, out _);
            _ = keys.Remove(key);
            _ = _observers.Remove(key);

            foreach (IObservable observable in _observablesWithPayload.Values)
            {
                observable.Remove(key);
            }
        }

#if UNITY_2019_1_OR_NEWER
        private void OnObserverDestroyed(ADestroyableObserver observer)
        {
            observer.onDestroyObservable.RemoveObserver(OnObserverDestroyed);

            long key = GetId(observer, out _);
            _ = keys.Remove(key);
            _ = _observers.Remove(key);

            foreach (IObservable observable in _observablesWithPayload.Values)
            {
                observable.Remove(key);
            }
        }
#endif

        internal long GetId(object observer, out bool firstTime)
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
    }

    interface IObservable
    {
        void Remove(long key);
        void ClearObservers();
    }

    public class Observable<TObserved, TPayload> : IObservable
    {
        private readonly Dictionary<long, List<IObserverInfo>> _observersWithPayload = new Dictionary<long, List<IObserverInfo>>();
        private readonly Observable<TObserved> _observable = default;

        public Observable<TObserved, TPayload> observable
        {
            get => this;
            set { }
        }

        public static Observable<TObserved, TPayload> operator +(Observable<TObserved, TPayload> observable, Action<TObserved, TPayload> action)
        {
            observable.Observe(action);
            return observable;
        }

        public static Observable<TObserved, TPayload> operator -(Observable<TObserved, TPayload> observable, Action<TObserved, TPayload> action)
        {
            observable.RemoveObserver(action);
            return observable;
        }

        internal Observable(Observable<TObserved> observable)
        {
            _observable = observable;
        }

        public void Observe(Action<TObserved, TPayload> action, bool willBeUnregisteredManually = false)
        {
            object observer = action.Target;

            Observe(observer, action, willBeUnregisteredManually);
        }

        public void Observe(object observer, Action<TObserved, TPayload> action, bool willBeUnregisteredManually = false)
        {
            long key = _observable.GetId(observer, out bool shouldAdd);

            if (_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list))
            {
                if (Contains(list, action)) return;

                list.Add(new ObserverPayloadInfo<TObserved, TPayload>(action));

                return;
            }

            IObserverInfo observerInfo = new ObserverPayloadInfo<TObserved, TPayload>(action);
            List<IObserverInfo> newList = new List<IObserverInfo> { observerInfo };
            _observersWithPayload.Add(key, newList);

            if (shouldAdd) _observable.keys.Add(key);

            _observable.SetupDestructor(observer, willBeUnregisteredManually);
        }

        public void RemoveObserver(Action<TObserved, TPayload> action)
        {
            object observer = action.Target;

            RemoveObserver(observer, action);
        }

        public void RemoveObserver(object observer, Action<TObserved, TPayload> action)
        {
            long key = _observable.GetId(observer, out _);

            if (!_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) return;

            for (int index = 0; index < list.Count; index++)
            {
                IObserverInfo current = list[index];

                if (!(current is ObserverPayloadInfo<TObserved, TPayload> observerPayloadInfo)) continue;

                observerPayloadInfo.Unregister(action);
            }
        }

        public static void InvokeMessage(Observable<TObserved, TPayload> observable, TObserved observed, TPayload payload)
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

        private void NotifyObservers(TObserved observed, TPayload payload)
        {
            for (int index = 0; index < _observable.keys.Count; index++)
            {
                long key = _observable.keys[index];

                if (!_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) continue;

                for (int observerIndex = 0; observerIndex < list.Count; observerIndex++)
                {
                    IObserverInfo observerInfo = list[observerIndex];

                    if (!(observerInfo is ObserverPayloadInfo<TObserved, TPayload> observerPayloadInfo)) continue;

                    observerPayloadInfo?.Invoke(observed, payload);
                }
            }
        }

        private bool Contains(List<IObserverInfo> list, Action<TObserved, TPayload> action)
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

        #region IObservable

        void IObservable.Remove(long key)
        {
            _ = _observersWithPayload.Remove(key);
        }

        void IObservable.ClearObservers()
        {
            _observersWithPayload.Clear();
        }

        #endregion
    }

    class IdHolder
    {
        public readonly long id;

        public IdHolder(long id) => this.id = id;
    }
}
