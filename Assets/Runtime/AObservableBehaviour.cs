#if UNITY_2019_1_OR_NEWER

using UnityEngine;

namespace Observables
{
    public abstract class AObservableBehaviour : MonoBehaviour
    {
        public readonly Observable<AObservableBehaviour> onDestroyObservable = new Observable<AObservableBehaviour>();

        protected virtual void OnDestroy()
        {
            Logger.Log($"OnDestroy() called for {this}");

            Observable<AObservableBehaviour>.InvokeMessage(onDestroyObservable, this);
        }
    }
}

#endif
