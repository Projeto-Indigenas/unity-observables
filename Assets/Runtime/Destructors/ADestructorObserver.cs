using Observables.Logging;

namespace Observables.Destructors
{
    public abstract class ADestructorObserver
    {
        internal readonly Observable destructorObservable = new Observable();

        ~ADestructorObserver()
        {
            Logger.Log($"~ADestructorObservable() called for {this}");

            destructorObservable.InvokeMessage(this);
        }
    }
}