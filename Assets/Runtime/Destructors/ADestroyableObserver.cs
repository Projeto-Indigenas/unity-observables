#if UNITY_2019_1_OR_NEWER

using UnityEngine;
using Logger = Observables.Logging.Logger;

namespace Observables.Destructors
{
    public abstract class ADestroyableObserver : MonoBehaviour
    {
        public readonly Observable<ADestroyableObserver> onDestroyObservable = new Observable<ADestroyableObserver>();

        protected virtual void OnDestroy()
        {
            Logger.Log($"OnDestroy() called for {this}");

            Observable<ADestroyableObserver>.InvokeMessage(onDestroyObservable, this);
        }
    }
}

#endif
