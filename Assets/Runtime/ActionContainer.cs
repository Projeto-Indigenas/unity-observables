using System;

namespace Observables
{
    internal abstract class AActionContainer : IEquatable<AActionContainer>
    {
        public abstract object target { get; }
        public abstract bool isValid { get; }
        public abstract bool Equals(AActionContainer other);
        public abstract void Invoke(object[] args);
        public static implicit operator AActionContainer(Action action) => new ActionContainer(action);

        public TAction GetAction<TAction>(WeakReference<TAction> weakRef) where TAction : class
        {
            if (!weakRef.TryGetTarget(out TAction action)) return null;
            return action;
        }

        public bool Equals<TAction>(WeakReference<TAction> weakRef1, WeakReference<TAction> weakRef2)
            where TAction : class
        {
            TAction action1 = GetAction(weakRef1);
            TAction action2 = GetAction(weakRef2);
            if (action1 == null || action2 == null) return false;
            return action1.Equals(action2);
        }
    }

    internal class ActionContainer : AActionContainer, IEquatable<ActionContainer>
    {
        private readonly WeakReference<Action> _action;
        public override object target => GetAction(_action)?.Target;
        public override bool isValid => _action.TryGetTarget(out _);
        public ActionContainer(Action action) => _action = new WeakReference<Action>(action);
        public static implicit operator ActionContainer(Action action) => new ActionContainer(action);
        public override void Invoke(object[] _) => GetAction(_action)?.Invoke();
        public bool Equals(ActionContainer other) => Equals(_action, other._action);
        public override bool Equals(AActionContainer other)
        {
            if (!(other is ActionContainer container)) return false;
            return Equals(container);
        }
    }

    internal class ActionContainer<TParam> : AActionContainer, IEquatable<ActionContainer<TParam>>
    {
        private readonly WeakReference<Action<TParam>> _action = default;
        public override object target => GetAction(_action)?.Target;
        public override bool isValid => _action.TryGetTarget(out _);
        public ActionContainer(Action<TParam> action) => _action = new WeakReference<Action<TParam>>(action);
        public static implicit operator ActionContainer<TParam>(Action<TParam> action)
            => new ActionContainer<TParam>(action);
        public override void Invoke(object[] args) => GetAction(_action)?.Invoke((TParam)args[0]);
        public bool Equals(ActionContainer<TParam> other) => Equals(_action, other._action);
        public override bool Equals(AActionContainer other)
        {
            if (!(other is ActionContainer<TParam> container)) return false;
            return Equals(container);
        }
    }

    internal class ActionContainer<TParam1, TParam2> : AActionContainer, IEquatable<ActionContainer<TParam1, TParam2>>
    {
        private readonly WeakReference<Action<TParam1, TParam2>> _action = default;
        public override object target => GetAction(_action)?.Target;
        public override bool isValid => _action.TryGetTarget(out _);
        public ActionContainer(Action<TParam1, TParam2> action) => _action = new WeakReference<Action<TParam1, TParam2>>(action);
        public static implicit operator ActionContainer<TParam1, TParam2>(Action<TParam1, TParam2> action)
            => new ActionContainer<TParam1, TParam2>(action);
        public override void Invoke(object[] args) => GetAction(_action)?.Invoke((TParam1)args[0], (TParam2)args[1]);
        public bool Equals(ActionContainer<TParam1, TParam2> other) => Equals(_action ,other._action);
        public override bool Equals(AActionContainer other)
        {
            if (!(other is ActionContainer<TParam1, TParam2> container)) return false;
            return Equals(container);
        }
    }

    internal class ActionContainer<TParam1, TParam2, TParam3> : AActionContainer, IEquatable<ActionContainer<TParam1, TParam2, TParam3>>
    {
        private readonly WeakReference<Action<TParam1, TParam2, TParam3>> _action = default;
        public override object target => GetAction(_action)?.Target;
        public override bool isValid => _action.TryGetTarget(out _);
        public ActionContainer(Action<TParam1, TParam2, TParam3> action) => _action = new WeakReference<Action<TParam1, TParam2, TParam3>>(action);
        public static implicit operator ActionContainer<TParam1, TParam2, TParam3>(Action<TParam1, TParam2, TParam3> action)
           => new ActionContainer<TParam1, TParam2, TParam3>(action);
        public override void Invoke(object[] args) => GetAction(_action)?.Invoke((TParam1)args[0], (TParam2)args[1], (TParam3)args[2]);
        public bool Equals(ActionContainer<TParam1, TParam2, TParam3> other) => Equals(_action, other._action);
        public override bool Equals(AActionContainer other)
        {
            if (!(other is ActionContainer<TParam1, TParam2, TParam3> container)) return false;
            return Equals(container);
        }
    }
}
