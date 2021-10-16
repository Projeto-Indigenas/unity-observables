using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Observables.Extensions;

#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
#endif

namespace Observables
{
    public class Observable<TObserved>
    {
        private static readonly Predicate<WeakReference<object>> _isObserverPredicate = default;
        private static readonly Predicate<WeakReference<Action<TObserved>>> _isSameActionPredicate = default;
        private static readonly Predicate<IObserverInfo> _isSameObserverInfoPredicate = default;
        private static object _searchingObserver = default;
        private static Action<TObserved> _searchingAction = default;
        private static IObserverInfo _searchingObserverInfo = default;

        private readonly List<WeakReference<object>> _keys = new List<WeakReference<object>>();
        private readonly Dictionary<WeakReference<object>, List<WeakReference<Action<TObserved>>>> _observers = new Dictionary<WeakReference<object>, List<WeakReference<Action<TObserved>>>>();
        private readonly Dictionary<WeakReference<object>, List<IObserverInfo>> _observersWithPayload = new Dictionary<WeakReference<object>, List<IObserverInfo>>();

        static Observable()
        {
            _isObserverPredicate = each => each.TryGetTarget(out object target) && target.Equals(_searchingObserver);
            _isSameActionPredicate = each => each.TryGetTarget(out Action<TObserved> target) && target.Equals(_searchingAction);
            _isSameObserverInfoPredicate = each => Equals(each, _searchingObserverInfo);
        }
        
        public void Observe(object observer, Action<TObserved> action)
        {
            WeakReference<object> key = _keys.GetWeakReferenceByInstance(observer);

            if (key != null && _observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list))
            {
                if (Contains(list, action)) return;

                list.Add(new WeakReference<Action<TObserved>>(action, true));

                return;
            }

            WeakReference<Action<TObserved>> weakAction = new WeakReference<Action<TObserved>>(action, true);
            List<WeakReference<Action<TObserved>>> newList = new List<WeakReference<Action<TObserved>>> { weakAction };

            if (key != null) _observers.Add(key, newList);
            else
            {
                key = new WeakReference<object>(observer);
                _observers.Add(key, newList);
                _keys.Add(key);
            }
        }

        public void Observe<TPayload>(object observer, Action<TObserved, TPayload> action)
        {
            WeakReference<object> key = _keys.GetWeakReferenceByInstance(observer);

            if (key != null && _observersWithPayload.TryGetValue(key, out List<IObserverInfo> list))
            {
                if (Contains(list, action)) return;

                list.Add(new ObserverPayloadInfo<TPayload>(action));

                return;
            }

            IObserverInfo observerInfo = new ObserverPayloadInfo<TPayload>(action);
            List<IObserverInfo> newList = new List<IObserverInfo> { observerInfo };

            if (key != null) _observersWithPayload.Add(key, newList);
            else
            {
                key = new WeakReference<object>(observer);
                _observersWithPayload.Add(key, newList);
                _keys.Add(key);
            }
        }

        public void StopObserving(object observer, Action<TObserved> action)
        {
            WeakReference<object> key = _keys.GetWeakReferenceByInstance(observer);

            if (key == null || !_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list)) return;

            _searchingAction = action;
            list.RemoveWhere(_isSameActionPredicate);
            _searchingAction = null;
        }

        public void StopObserving<TPayload>(object observer, Action<TObserved, TPayload> action)
        {
            WeakReference<object> key = _keys.GetWeakReferenceByInstance(observer);

            if (key == null || !_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) return;

            _searchingObserverInfo = new ObserverPayloadInfo<TPayload>.ActionContainer(action);
            list.RemoveWhere(_isSameObserverInfoPredicate);
            _searchingObserverInfo = null;
        }

        public void StopObservingAll(object observer)
        {
            WeakReference<object> key = _keys.GetWeakReferenceByInstance(observer);

            if (key == null) return;

            _keys.Remove(key);

            _observers.Remove(key);
            _observersWithPayload.Remove(key);
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

        private void SetupDestructors(object observer, bool
#if OBSERVABLES_DEVELOPMENT
            manuallyDestroyed
#else
            _
#endif
            )
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
            if (isObservable || manuallyDestroyed) return;

            Logger.Log($"Observer is not either DestructorObservable nor ObservableBehaviour. " +
                $"It should inherit from one of them or stop observing manually.\n" +
                $"Observable type: {observer}");
#endif
        }

        private bool Contains(List<WeakReference<Action<TObserved>>> list, Action<TObserved> action)
        {
            for (int index = 0; index < list.Count; index++)
            {
                WeakReference<Action<TObserved>> current = list[index];

                if (current.TryGetTarget(out Action<TObserved> target) && target == action)
                {
                    return true;
                }
            }

            return false;
        }

        private bool Contains<TPayload>(List<IObserverInfo> list, Action<TObserved, TPayload> action)
        {
            for (int index = 0; index < list.Count; index++)
            {
                IObserverInfo current = list[index];
                if (current is ObserverPayloadInfo<TPayload> info && info.Equals(action))
                {
                    return true;
                }
            }
            return false;
        }

        private void NotifyObservers(TObserved observed)
        {
            for (int index = 0; index < _keys.Count; index++)
            {
                WeakReference<object> key = _keys[index];

                if (!_observers.TryGetValue(key, out List<WeakReference<Action<TObserved>>> list)) continue;

                for (int observerIndex = 0; observerIndex < list.Count; observerIndex++)
                {
                    if (!list[observerIndex].TryGetTarget(out Action<TObserved> current)) continue;

                    current?.Invoke(observed);
                }
            }
        }

        private void NotifyObservers<TPayload>(TObserved observed, TPayload payload)
        {
            for (int index = 0; index < _keys.Count; index++)
            {
                WeakReference<object> key = _keys[index];

                if (!_observersWithPayload.TryGetValue(key, out List<IObserverInfo> list)) continue;

                for (int observerIndex = 0; observerIndex < list.Count; observerIndex++)
                {
                    ObserverPayloadInfo<TPayload> current = (ObserverPayloadInfo<TPayload>)list[observerIndex];

                    if (!current.weakAction.TryGetTarget(out Action<TObserved, TPayload> target)) continue;

                    target.Invoke(observed, payload);
                }
            }
        }

        private void OnObserverDestructed(ADestructorObserver observer)
        {
            observer.destructorObservable.StopObserving(this, OnObserverDestructed);

            StopObservingAll(observer);
        }

#if UNITY_INCLUDE_TESTS
        private void OnObserverDestroyed(AObservableBehaviour observer)
        {
            observer.onDestroyObservable.StopObserving(this, OnObserverDestroyed);

            StopObservingAll(observer);
        }
#endif

        public interface IObserverInfo { }

        internal readonly struct ObserverPayloadInfo<TPayload> : IObserverInfo, IEquatable<Action<TObserved, TPayload>>, IEquatable<ObserverPayloadInfo<TPayload>.ActionContainer>
        {
            public readonly WeakReference<Action<TObserved, TPayload>> weakAction;

            public ObserverPayloadInfo(Action<TObserved, TPayload> action)
            {
                weakAction = new WeakReference<Action<TObserved, TPayload>>(action);
            }

            public void Invoke(TObserved observed, TPayload payload)
            {
                if (!weakAction.TryGetTarget(out Action<TObserved, TPayload> action)) return;

                action.Invoke(observed, payload);
            }

            public bool Equals(Action<TObserved, TPayload> other)
            {
                if (!weakAction.TryGetTarget(out Action<TObserved, TPayload> target)) return false;

                return target.Equals(other);
            }

            public bool Equals(ActionContainer container)
            {
                return Equals(container.action);
            }

            internal readonly struct ActionContainer : IObserverInfo
            {
                public readonly Action<TObserved, TPayload> action;

                public ActionContainer(Action<TObserved, TPayload> action)
                {
                    this.action = action;
                }
            }
        }
    }
}
