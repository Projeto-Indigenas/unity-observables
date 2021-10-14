namespace Observables
{
    public abstract class DestructorObservable
    {
        internal readonly Observable<DestructorObservable> destructorObservable = new Observable<DestructorObservable>();

        ~DestructorObservable()
        {
#if ENABLE_OBSERVABLES_LOGS
            Debug.Log($"OnDestroy called for {this}");
#endif
            Observable<DestructorObservable>.InvokeMessage(destructorObservable, this);
        }
    }
}