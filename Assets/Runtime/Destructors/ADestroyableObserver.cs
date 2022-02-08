#if UNITY_2019_1_OR_NEWER

using UnityEngine;
using Logger = Observables.Logging.Logger;

namespace Observables.Destructors
{
    public abstract class ADestroyableObserver : MonoBehaviour
    {
        internal readonly Observable onDestroyObservable = new Observable();

        protected virtual void OnDestroy()
        {
            Logger.Log($"OnDestroy() called for {this}");

            onDestroyObservable.InvokeMessage(this);
        }
    }
}

#endif
