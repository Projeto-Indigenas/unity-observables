using System;
using System.Collections.Generic;
using Observables.Extensions;
using UnityEngine;

namespace Observables
{
    public class Observable<TObserved>
    {
        private readonly List<object> _observersToRemove = new List<object>();
        private readonly Dictionary<object, ObserverInfoLists> _observers = new Dictionary<object, ObserverInfoLists>();
        private readonly Predicate<object> _isObserverInfoPredicate;

        private bool _isIterating = default;
        private object _searchingObserver = default;

        public Observable()
        {
            _isObserverInfoPredicate = each => Equals(each, _searchingObserver);
        }
        
        public void Observe(object observer, Action<TObserved> action, bool manuallyDestroyed = false)
        {
            if (observer == this || 
                _observers.TryGetValue(observer, out ObserverInfoLists lists) && Contains(observer, lists.observers))
            {
                return;
            }

            AddObserver(observer, action);

            SetupDestructors(observer, manuallyDestroyed);
        }

        public void Observe<TPayload>(object observer, Action<TObserved, TPayload> action, bool manuallyDestroyed = false)
        {
            if (observer == this || 
                _observers.TryGetValue(observer, out ObserverInfoLists lists) && Contains(observer, lists.observersWithPayload))
            {
                return;
            }

            AddObserver(observer, action);

            SetupDestructors(observer, manuallyDestroyed);
        }

        public void StopObserving(object observer)
        {
            if (_isIterating)
            {
                _observersToRemove.Add(observer);

                return;
            }

            _ = _observers.Remove(observer);
        }

        public static void InvokeMessage(Observable<TObserved> observable, TObserved payload)
        {
#if UNITY_EDITOR
            try
            {
#endif
                observable.NotifyObservers(payload);
#if UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Debug.Log($"Unexpected exception when invoking message. \n" +
                    $"observed -> {payload}\n" +
                    $"exception -> {ex}");
            }
#endif
        }

        public static void InvokeMessage<TPayload>(Observable<TObserved> observable, TObserved observed, TPayload payload)
        {
            observable.NotifyObservers(observed, payload);
        }

        private void SetupDestructors(object observer, bool
#if UNITY_EDITOR || DEBUG
            manuallyDestroyed
#else
            _
#endif
            )
        {
            if (observer is DestructorObservable destructorObservable)
            {
                destructorObservable.destructorObservable.Observe(this, OnObserverDestructed);

                return;
            }

            if (observer is ObservableBehaviour observableBehaviour)
            {
                observableBehaviour.onDestroyObservable.Observe(this, OnObserverDestroyed);

                return;
            }

#if UNITY_EDITOR || DEBUG
            Type observerType = observer.GetType();
            bool isObservable = observerType.IsGenericType && observerType.GetGenericTypeDefinition() == typeof(Observable<>);
            if (isObservable || manuallyDestroyed) return;

            Debug.Log($"Observer is not either DestructorObservable nor ObservableBehaviour. " +
                $"It should inherit from one of them or stop observing manually.\n" +
                $"Observable type: {observer}");
#endif
        }

        private bool Contains(object observer, List<IObserverInfo> observers)
        {
            _searchingObserver = observer;
            bool contains = observers.Contains(_isObserverInfoPredicate);
            _searchingObserver = null;
            return contains;
        }

        private void AddObserver(object observer, Action<TObserved> action) 
        {
            ObserverInfo info = new ObserverInfo(observer, action);

            if (_observers.TryGetValue(observer, out ObserverInfoLists lists))
            {
                lists.observers.Add(info);
                return;
            }
                
            _observers[observer] = new ObserverInfoLists(new List<IObserverInfo> { info }, new List<IObserverInfo>());
        }

        private void AddObserver<TPayload>(object observer, Action<TObserved, TPayload> action)
        {
            ObserverPayloadInfo<TPayload> info = new ObserverPayloadInfo<TPayload>(observer, action);

            if (_observers.TryGetValue(observer, out ObserverInfoLists lists))
            {
                lists.observersWithPayload.Add(info);
                return;
            }

            _observers[observer] = new ObserverInfoLists(
                new List<IObserverInfo>(), 
                new List<IObserverInfo> { info }
            );
        }

        private void NotifyObservers(TObserved observed)
        {
            _isIterating = true;
            Dictionary<object, ObserverInfoLists>.Enumerator enumerator = _observers.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<object, ObserverInfoLists> pair = enumerator.Current;
                ObserverInfoLists lists = pair.Value;
                List<IObserverInfo> observerInfos = lists.observers;
                for (int index = 0; index < observerInfos.Count; index++)
                {
                    IObserverInfo info = observerInfos[index];
                    if (info is ObserverInfo observerInfo)
                    {
                        observerInfo.Invoke(observed);
                        continue;
                    }
                }
            }
            _isIterating = false;

            CleanUpObserversIfNeeded();
        }

        private void NotifyObservers<TPayload>(TObserved observed, TPayload payload)
        {
            _isIterating = true;
            Dictionary<object, ObserverInfoLists>.Enumerator enumerator = _observers.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<object, ObserverInfoLists> pair = enumerator.Current;
                ObserverInfoLists lists = pair.Value;
                List<IObserverInfo> observerInfos = lists.observersWithPayload;
                for (int index = 0; index < observerInfos.Count; index++)
                {
                    IObserverInfo info = observerInfos[index];
                    if (info is ObserverPayloadInfo<TPayload> observerInfo)
                    {
                        observerInfo.Invoke(observed, payload);
                    }
                }
            }
            _isIterating = false;

            CleanUpObserversIfNeeded();
        }

        private void CleanUpObserversIfNeeded()
        {
            if (_observersToRemove.Count == 0) return;

            for (int index = 0; index < _observersToRemove.Count; index++)
            {
                object observer = _observersToRemove[index];
                _ = _observers.Remove(observer);
            }

            _observersToRemove.Clear();
        }

        private void OnObserverDestructed(DestructorObservable destructorObservable)
        {
            StopObserving(destructorObservable);

            if (_isIterating) _observersToRemove.AddRange(_observers.Keys);
            else _observers.Clear();
        }

        private void OnObserverDestroyed(ObservableBehaviour destroyingBehaviour)
        {
            StopObserving(destroyingBehaviour);

            if (_isIterating) _observersToRemove.AddRange(_observers.Keys);
            else _observers.Clear();
        }

        private interface IObserverInfo
        {
            object observer { get; }
        }

        private readonly struct ObserverInfo : IObserverInfo
        {
            public object observer { get; }
            public readonly Action<TObserved> action;

            public ObserverInfo(object observer, Action<TObserved> action)
            {
                this.observer = observer;
                this.action = action;
            }

            public void Invoke(TObserved observed)
            {
                action?.Invoke(observed);
            }
        }

        private readonly struct ObserverPayloadInfo<TPayload> : IObserverInfo
        {
            public object observer { get; }
            public readonly Action<TObserved, TPayload> payloadAction;

            public ObserverPayloadInfo(object observer, Action<TObserved, TPayload> action)
            {
                this.observer = observer;
                payloadAction = action;
            }

            public void Invoke(TObserved observed, TPayload payload)
            {
                payloadAction?.Invoke(observed, payload);
            }
        }

        private readonly struct ObserverInfoLists
        {
            public readonly List<IObserverInfo> observers;
            public readonly List<IObserverInfo> observersWithPayload;

            public ObserverInfoLists(List<IObserverInfo> observers, List<IObserverInfo> observersWithPayload)
            {
                this.observers = observers;
                this.observersWithPayload = observersWithPayload;
            }
        }
    }
}
