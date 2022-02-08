using Observables.Destructors;
using Observables.Extensions;
using Observables.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Observables
{
    public class Observable 
    {
        private static readonly Predicate<WeakReference<AAction>> _searchActionPredicate = each =>
        {
            return each.TryGetTarget(out AAction target) && target.Equals(_searchingAction);
        };

        private static object[] _argumentsBuffer = new object[3];
        private static AAction _searchingAction = default;
        private static long _currentId = long.MinValue;

        private readonly ConditionalWeakTable<object, IdHolder> _keysIds = new ConditionalWeakTable<object, IdHolder>();
        private readonly Dictionary<long, List<WeakReference<AAction>>> _observers = new Dictionary<long, List<WeakReference<AAction>>>();
        private readonly Dictionary<Type, IObservable> _observablesWithPayload = new Dictionary<Type, IObservable>();

        private readonly Action<ADestructorObserver> _onDestructorCalled = default;
        private readonly Action<ADestroyableObserver> _onDestroyCalled = default;

        internal List<long> keys { get; } = new List<long>();

        public Observable()
        {
            _onDestructorCalled = OnObserverDestructed;
            _onDestroyCalled = OnObserverDestroyed;
        }

        public void Observe(Action action, bool willBeUnregisteredMannually = false) 
            => Observe(action.Target, action, willBeUnregisteredMannually);
        public void Observe<TParam>(Action<TParam> action, bool willBeUnregisteredMannually = false) 
            => Observe(action.Target, action, willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2>(Action<TParam1, TParam2> action, bool willBeUnregisteredMannually = false) 
            => Observe(action.Target, action, willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2, TParam3>(Action<TParam1, TParam2, TParam3> action, bool willBeUnregisteredMannually = false) 
            => Observe(action.Target, action, willBeUnregisteredMannually);
        public void Observe(object observer, Action action, bool willBeUnregisteredMannually = false) 
            => Observe(observer, new ActionContainer(action), willBeUnregisteredMannually);
        public void Observe<TParam>(object observer, Action<TParam> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam>(action), willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2>(object observer, Action<TParam1, TParam2> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam1, TParam2>(action), willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2, TParam3>(object observer, Action<TParam1, TParam2, TParam3> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam1, TParam2, TParam3>(action), willBeUnregisteredMannually);
        public void RemoveObserver(Action action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam>(Action<TParam> action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam1, TParam2>(Action<TParam1, TParam2> action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam1, TParam2, TParam3>(Action<TParam1, TParam2, TParam3> action)
            => RemoveObserver(action.Target, action);
        public void RemoveObserver(object observer, Action action)
            => RemoveObserver(observer, new ActionContainer(action));
        public void RemoveObserver<TParam>(object observer, Action<TParam> action)
            => RemoveObserver(observer, new ActionContainer<TParam>(action));
        public void RemoveObserver<TParam1, TParam2>(object observer, Action<TParam1, TParam2> action)
            => RemoveObserver(observer, new ActionContainer<TParam1, TParam2>(action));
        public void RemoveObserver<TParam1, TParam2, TParam3>(object observer, Action<TParam1, TParam2, TParam3> action)
            => RemoveObserver(observer, new ActionContainer<TParam1, TParam2, TParam3>(action));

        public void ClearObservers()
        {
            keys.Clear();
            _observers.Clear();

            foreach (IObservable each in _observablesWithPayload.Values)
            {
                each.ClearObservers();
            }
        }

        public void InvokeMessage<TParam>(TParam param)
        {
            _argumentsBuffer[0] = param;
            NotifyObservers(_argumentsBuffer);
            _argumentsBuffer[0] = null;
        }

        public void InvokeMessage<TParam1, TParam2>(TParam1 param1, TParam2 param2)
        {
            _argumentsBuffer[0] = param1;
            _argumentsBuffer[1] = param2;
            NotifyObservers(_argumentsBuffer);
            _argumentsBuffer[0] = null;
            _argumentsBuffer[1] = null;
        }

        public void InvokeMessage<TParam1, TParam2, TParam3>(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            _argumentsBuffer[0] = param1;
            _argumentsBuffer[1] = param2;
            _argumentsBuffer[2] = param3;
            NotifyObservers(_argumentsBuffer);
            _argumentsBuffer[0] = null;
            _argumentsBuffer[1] = null;
            _argumentsBuffer[2] = null;
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
                destructorObserver.destructorObservable.Observe(_onDestructorCalled);

                return;
            }

#if UNITY_2019_1_OR_NEWER
            if (observer is ADestroyableObserver observableBehaviour)
            {
                observableBehaviour.onDestroyObservable.Observe(_onDestroyCalled);

                return;
            }
#endif

#if OBSERVABLES_DEVELOPMENT
            Type observerType = observer.GetType();
            bool isObservable = observerType == typeof(Observable);
            if (isObservable || willBeUnregisteredManually) return;

            Logger.Log($"Observer is not either DestructorObservable nor ObservableBehaviour. " +
                $"It should inherit from one of them or stop observing manually.\n" +
                $"Observable type: {observer}");
#endif
        }

        private void Observe(object observer, AAction action, bool willBeUnregisteredManually = false)
        {
            long key = GetId(observer, out bool shouldAdd);

            if (_observers.TryGetValue(key, out List<WeakReference<AAction>> list))
            {
                if (Contains(list, action)) return;

                list.Add(new WeakReference<AAction>(action));

                return;
            }

            WeakReference<AAction> weakAction = new WeakReference<AAction>(action);
            List<WeakReference<AAction>> newList = new List<WeakReference<AAction>> { weakAction };
            _observers.Add(key, newList);

            if (shouldAdd) keys.Add(key);

            SetupDestructor(observer, willBeUnregisteredManually);
        }

        private void RemoveObserver(object observer, AAction action)
        {
            long key = GetId(observer, out _);

            if (!_observers.TryGetValue(key, out List<WeakReference<AAction>> list)) return;

            _searchingAction = action;
            _ = list.RemoveWhere(_searchActionPredicate);
            _searchingAction = null;
        }

        private bool Contains(List<WeakReference<AAction>> list, AAction action)
        {
            for (int index = list.Count - 1; index >= 0; index--)
            {
                WeakReference<AAction> current = list[index];

                if (!current.TryGetTarget(out AAction target))
                {
                    list.RemoveAt(index);

                    continue;
                }

                if (target.Equals(action)) return true;
            }

            return false;
        }

        private void NotifyObservers(object[] args)
        {
#if OBSERVABLES_DEVELOPMENT
            try
            {
#endif
                for (int index = 0; index < keys.Count; index++)
                {
                    long key = keys[index];

                    if (!_observers.TryGetValue(key, out List<WeakReference<AAction>> list)) continue;

                    for (int observerIndex = list.Count - 1; observerIndex >= 0; observerIndex--)
                    {
                        WeakReference<AAction> current = list[observerIndex];

                        if (!current.TryGetTarget(out AAction target))
                        {
                            list.RemoveAt(observerIndex);

                            continue;
                        }

                        target?.Invoke(args);
                    }
                }
#if OBSERVABLES_DEVELOPMENT
            }
            catch (Exception ex)
            {
                Logger.Log($"Unexpected exception when invoking message. \n" +
                    $"args -> {args}\n" +
                    $"exception -> {ex}");
            }
#endif
        }

        private void OnObserverDestructed(ADestructorObserver observer)
        {
            observer.destructorObservable.RemoveObserver(_onDestructorCalled);

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
            observer.onDestroyObservable.RemoveObserver(_onDestroyCalled);

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

    class IdHolder
    {
        public readonly long id;

        public IdHolder(long id) => this.id = id;
    }

    public abstract class AAction
    {
        public abstract object Target { get; }

        public abstract void Invoke(object[] args);

        public static implicit operator AAction(Action action) => new ActionContainer(action);
    }

    public class ActionContainer : AAction, IEquatable<ActionContainer>
    {
        private readonly Action _action;

        public override object Target => _action.Target;

        public ActionContainer(Action other)
        {
            _action = other;
        }

        public static implicit operator ActionContainer(Action action) => new ActionContainer(action);

        public override void Invoke(object[] _) => _action?.Invoke();

        bool IEquatable<ActionContainer>.Equals(ActionContainer other)
        {
            return _action == other._action;
        }
    }

    public class ActionContainer<TParam> : AAction, IEquatable<ActionContainer<TParam>>
    {
        private readonly Action<TParam> _action = default;

        public override object Target => _action.Target;

        public ActionContainer(Action<TParam> action)
        {
            _action = action;
        }

        public static implicit operator ActionContainer<TParam>(Action<TParam> action) 
            => new ActionContainer<TParam>(action);

        public override void Invoke(object[] args) => _action?.Invoke((TParam)args[0]);

        bool IEquatable<ActionContainer<TParam>>.Equals(ActionContainer<TParam> other)
        {
            return _action == other._action;
        }
    }

    public class ActionContainer<TParam1, TParam2> : AAction, IEquatable<ActionContainer<TParam1, TParam2>>
    {
        private readonly Action<TParam1, TParam2> _action = default;

        public override object Target => _action.Target;

        public ActionContainer(Action<TParam1, TParam2> action)
        {
            _action = action;
        }

        public static implicit operator ActionContainer<TParam1, TParam2>(Action<TParam1, TParam2> action) 
            => new ActionContainer<TParam1, TParam2>(action);

        public override void Invoke(object[] args) => _action?.Invoke((TParam1)args[0], (TParam2)args[1]);

        bool IEquatable<ActionContainer<TParam1, TParam2>>.Equals(ActionContainer<TParam1, TParam2> other)
        {
            return _action == other._action;
        }
    }

    public class ActionContainer<TParam1, TParam2, TParam3> : AAction, IEquatable<ActionContainer<TParam1, TParam2, TParam3>>
    {
        private readonly Action<TParam1, TParam2, TParam3> _action = default;

        public override object Target => _action;

        public ActionContainer(Action<TParam1, TParam2, TParam3> action)
        {
            _action = action;
        }

        public static implicit operator ActionContainer<TParam1, TParam2, TParam3>(Action<TParam1, TParam2, TParam3> action)
           => new ActionContainer<TParam1, TParam2, TParam3>(action);

        public override void Invoke(object[] args) => _action?.Invoke((TParam1)args[0], (TParam2)args[1], (TParam3)args[2]);

        bool IEquatable<ActionContainer<TParam1, TParam2, TParam3>>.Equals(ActionContainer<TParam1, TParam2, TParam3> other)
        {
            return _action == other._action;
        }
    }
}
