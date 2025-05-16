using System;

namespace FlexiLog.Interfaces
{
    public interface IScheduler : IDisposable
    {
        event EventHandler<Exception> TimerExceptionEvent;
        void Start(Action action, bool runImmediately = true);
        void Stop();
    }
}
