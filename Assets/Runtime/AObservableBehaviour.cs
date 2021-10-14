using UnityEngine;

namespace Observables
{
    public abstract class AObservableBehaviour : MonoBehaviour
    {
        public readonly Observable<AObservableBehaviour> onDestroyObservable = new Observable<AObservableBehaviour>();

        protected virtual void OnDestroy()
        {
#if ENABLE_OBSERVABLES_LOGS
            Debug.Log($"OnDestroy() called for {this}");
#endif
            Observable<AObservableBehaviour>.InvokeMessage(onDestroyObservable, this);
        }
    }
}
