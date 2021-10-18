using Observables.Logging;

namespace Observables.Destructors
{
    public abstract class ADestructorObserver
    {
        private readonly Observable<ADestructorObserver> _destructorObservable = new Observable<ADestructorObserver>();

        public Observable<ADestructorObserver> destructorObservable
        {
            get => _destructorObservable;
            set { }
        }

        ~ADestructorObserver()
        {
            Logger.Log($"~ADestructorObservable() called for {this}");

            Observable<ADestructorObserver>.InvokeMessage(destructorObservable, this);
        }
    }
}