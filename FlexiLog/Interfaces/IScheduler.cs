﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiLog.Interfaces
{
    public interface IScheduler : IDisposable
    {
        event EventHandler<Exception> TimerExceptionEvent;

        /// <summary>
        /// CancellationToken이 필요하지 않은 간단한 action 수행할 때를 위한 편의 래핑 함수
        /// </summary>
        void Start(Action action, bool runImmediately = true);

        void Start(Action<CancellationToken> action, bool runImmediately = true);

        void Stop();
    }
}
