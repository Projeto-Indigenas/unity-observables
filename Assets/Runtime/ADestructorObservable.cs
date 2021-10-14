namespace Observables
{
    public abstract class ADestructorObservable
    {
        internal readonly Observable<ADestructorObservable> destructorObservable = new Observable<ADestructorObservable>();

        ~ADestructorObservable()
        {
#if ENABLE_OBSERVABLES_LOGS
            UnityEngine.Debug.Log($"~ADestructorObservable() called for {this}");
#endif
            Observable<ADestructorObservable>.InvokeMessage(destructorObservable, this);
        }
    }
}