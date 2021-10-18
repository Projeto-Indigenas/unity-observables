#if UNITY_2019_1_OR_NEWER

using UnityEngine;
using Logger = Observables.Logging.Logger;

namespace Observables.Destructors
{
    public abstract class ADestroyableObserver : MonoBehaviour
    {
        private readonly Observable<ADestroyableObserver> _onDestroyObservable = new Observable<ADestroyableObserver>();

        public Observable<ADestroyableObserver> onDestroyObservable
        {
            get => _onDestroyObservable;
            set { }
        }

        protected virtual void OnDestroy()
        {
            Logger.Log($"OnDestroy() called for {this}");

            Observable<ADestroyableObserver>.InvokeMessage(onDestroyObservable, this);
        }
    }
}

#endif
