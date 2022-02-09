using Observables.Logging;

namespace Observables.Destructors
{
    public abstract class ADestructorObserver
    {
        public readonly Observable destructorObservable = new Observable();

        ~ADestructorObserver()
        {
            Logger.Log($"~ADestructorObservable() called for {this}");

            destructorObservable.InvokeMessage(this);
        }
    }
}