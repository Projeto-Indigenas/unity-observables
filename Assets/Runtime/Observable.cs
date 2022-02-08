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
        private static readonly Predicate<AActionContainer> _searchActionPredicate = each =>
        {
            return each.Equals(_searchingAction);
        };

        private static AActionContainer _searchingAction = default;
        private static object[] _argumentsBuffer = new object[3];
        private static long _currentId = long.MinValue;

        private readonly List<long> _keys = new List<long>();
        private readonly ConditionalWeakTable<object, IdHolder> _keysIds = new ConditionalWeakTable<object, IdHolder>();
        private readonly Dictionary<long, List<AActionContainer>> _observersNoParam = new Dictionary<long, List<AActionContainer>>();
        private readonly Dictionary<long, List<AActionContainer>> _observersOneParam = new Dictionary<long, List<AActionContainer>>();
        private readonly Dictionary<long, List<AActionContainer>> _observersTwoParam = new Dictionary<long, List<AActionContainer>>();
        private readonly Dictionary<long, List<AActionContainer>> _observersThreeParam = new Dictionary<long, List<AActionContainer>>();

        private readonly Action<ADestructorObserver> _onDestructorCalled = default;
        private readonly Action<ADestroyableObserver> _onDestroyCalled = default;


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
            => Observe(observer, new ActionContainer(action), _observersNoParam, willBeUnregisteredMannually);
        public void Observe<TParam>(object observer, Action<TParam> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam>(action), _observersOneParam, willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2>(object observer, Action<TParam1, TParam2> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam1, TParam2>(action), _observersTwoParam, willBeUnregisteredMannually);
        public void Observe<TParam1, TParam2, TParam3>(object observer, Action<TParam1, TParam2, TParam3> action, bool willBeUnregisteredMannually = false)
            => Observe(observer, new ActionContainer<TParam1, TParam2, TParam3>(action), _observersThreeParam, willBeUnregisteredMannually);
        public void RemoveObserver(Action action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam>(Action<TParam> action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam1, TParam2>(Action<TParam1, TParam2> action) 
            => RemoveObserver(action.Target, action);
        public void RemoveObserver<TParam1, TParam2, TParam3>(Action<TParam1, TParam2, TParam3> action)
            => RemoveObserver(action.Target, action);
        public void RemoveObserver(object observer, Action action)
            => RemoveObserver(observer, new ActionContainer(action), _observersNoParam);
        public void RemoveObserver<TParam>(object observer, Action<TParam> action)
            => RemoveObserver(observer, new ActionContainer<TParam>(action), _observersOneParam);
        public void RemoveObserver<TParam1, TParam2>(object observer, Action<TParam1, TParam2> action)
            => RemoveObserver(observer, new ActionContainer<TParam1, TParam2>(action), _observersTwoParam);
        public void RemoveObserver<TParam1, TParam2, TParam3>(object observer, Action<TParam1, TParam2, TParam3> action)
            => RemoveObserver(observer, new ActionContainer<TParam1, TParam2, TParam3>(action), _observersThreeParam);

        public void ClearObservers()
        {
            _keys.Clear();
            _observersNoParam.Clear();
            _observersOneParam.Clear();
            _observersTwoParam.Clear();
            _observersThreeParam.Clear();
        }

        public void InvokeMessage()
        {
            NotifyObservers(null, _observersNoParam);
        }

        public void InvokeMessage<TParam>(TParam param)
        {
            _argumentsBuffer[0] = param;
            NotifyObservers(_argumentsBuffer, _observersOneParam);
            _argumentsBuffer[0] = null;
        }

        public void InvokeMessage<TParam1, TParam2>(TParam1 param1, TParam2 param2)
        {
            _argumentsBuffer[0] = param1;
            _argumentsBuffer[1] = param2;
            NotifyObservers(_argumentsBuffer, _observersTwoParam);
            _argumentsBuffer[0] = null;
            _argumentsBuffer[1] = null;
        }

        public void InvokeMessage<TParam1, TParam2, TParam3>(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            _argumentsBuffer[0] = param1;
            _argumentsBuffer[1] = param2;
            _argumentsBuffer[2] = param3;
            NotifyObservers(_argumentsBuffer, _observersThreeParam);
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

        private void Observe(object observer, AActionContainer action, Dictionary<long, List<AActionContainer>> dict, bool willBeUnregisteredManually = false)
        {
            long key = GetId(observer, out bool shouldAdd);

            if (dict.TryGetValue(key, out List<AActionContainer> list))
            {
                if (Contains(list, action)) return;

                list.Add(action);

                return;
            }

            List<AActionContainer> newList = new List<AActionContainer> { action };
            dict.Add(key, newList);

            if (shouldAdd) _keys.Add(key);

            SetupDestructor(observer, willBeUnregisteredManually);
        }

        private void RemoveObserver(object observer, AActionContainer action, Dictionary<long, List<AActionContainer>> dict)
        {
            long key = GetId(observer, out _);

            if (!dict.TryGetValue(key, out List<AActionContainer> list)) return;

            _searchingAction = action;
            _ = list.RemoveWhere(_searchActionPredicate);
            _searchingAction = null;
        }

        private bool Contains(List<AActionContainer> list, AActionContainer action)
        {
            for (int index = 0; index < list.Count; index++)
            {
                AActionContainer current = list[index];

                if (current.Equals(action)) return true;
            }

            return false;
        }

        private void NotifyObservers(object[] args, Dictionary<long, List<AActionContainer>> dict)
        {
#if OBSERVABLES_DEVELOPMENT
            try
            {
#endif
                for (int index = 0; index < _keys.Count; index++)
                {
                    long key = _keys[index];

                    if (!dict.TryGetValue(key, out List<AActionContainer> list)) continue;

                    for (int observerIndex = 0; observerIndex < list.Count; observerIndex++)
                    {
                        AActionContainer current = list[observerIndex];

                        current?.Invoke(args);
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
            _ = _keys.Remove(key);
            _ = _observersNoParam.Remove(key);
            _ = _observersOneParam.Remove(key);
            _ = _observersTwoParam.Remove(key);
            _ = _observersThreeParam.Remove(key);
        }

#if UNITY_2019_1_OR_NEWER
        private void OnObserverDestroyed(ADestroyableObserver observer)
        {
            observer.onDestroyObservable.RemoveObserver(_onDestroyCalled);

            long key = GetId(observer, out _);
            _ = _keys.Remove(key);
            _ = _observersNoParam.Remove(key);
            _ = _observersOneParam.Remove(key);
            _ = _observersTwoParam.Remove(key);
            _ = _observersThreeParam.Remove(key);
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

    class IdHolder
    {
        public readonly long id;
        public IdHolder(long id) => this.id = id;
    }
}
