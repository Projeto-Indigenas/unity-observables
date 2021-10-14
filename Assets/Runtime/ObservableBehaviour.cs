using UnityEngine;

namespace Observables
{
    public class ObservableBehaviour : MonoBehaviour
    {
        public readonly Observable<ObservableBehaviour> onDestroyObservable = new Observable<ObservableBehaviour>();

        protected virtual void OnDestroy()
        {
#if ENABLE_OBSERVABLES_LOGS
            Debug.Log($"OnDestroy called for {this}");
#endif
            Observable<ObservableBehaviour>.InvokeMessage(onDestroyObservable, this);
        }
    }
}
