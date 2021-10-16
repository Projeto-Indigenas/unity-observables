namespace Observables
{
    public abstract class ADestructorObserver
    {
        internal readonly Observable<ADestructorObserver> destructorObservable = new Observable<ADestructorObserver>();

        ~ADestructorObserver()
        {
            Logger.Log($"~ADestructorObservable() called for {this}");
            Observable<ADestructorObserver>.InvokeMessage(destructorObservable, this);
        }
    }
}