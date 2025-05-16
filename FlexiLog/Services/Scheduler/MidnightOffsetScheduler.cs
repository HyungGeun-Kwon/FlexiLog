using System;
using System.Threading;
using System.Threading.Tasks;
using FlexiLog.Interfaces;

namespace FlexiLog.Services.Scheduler
{
    public class MidnightOffsetScheduler : IScheduler
    {
        private Timer _timer;
        private readonly object _startLock = new object();
        private Action<CancellationToken> _action;
        private DateTime _lastRunDate;

        private CancellationTokenSource _cts;
        private int _running; // o : 실행X(false) 1 : 실행(true)


        /// <summary>
        /// 타이머가 매우 정확하지는 않으므로 자정보다 미세하게 일찍 실행될 수 있어 안정성을 위해 Offset 설정
        /// 물론 과거 타이머 실행 날짜와 현재 실행 날짜가 동일하다면 타이머 시간 다시 계산해서 활성화
        /// 기본값 = 10sec
        /// </summary>
        public TimeSpan MidnightOffset { get; set; } = TimeSpan.FromSeconds(10);

        public event EventHandler<Exception> TimerExceptionEvent;

        public void Start(Action action, bool runImmediately = true)
        {
            Start(_ => action(), runImmediately);
        }

        public void Start(Action<CancellationToken> action, bool runImmediately = true)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            lock (_startLock)
            {
                if (_timer != null) throw new InvalidOperationException("Scheduler already started.");

                _cts = new CancellationTokenSource();
                _action = action;
                var due = runImmediately ? TimeSpan.Zero : GetDelayUntilNext();

                // 타이머 내부에서 다음 스케줄링
                _timer = new Timer(OnTimerTick, null, due, Timeout.InfiniteTimeSpan);
            }
        }

        private void OnTimerTick(object _)
        {
            // 재진입 방지
            if (Interlocked.Exchange(ref _running, 1) == 1) return;

            try
            {
                // 동일한날에 실행되었다면 자정에 정확하게 실행되도록 타이머 변경

                if (_cts.IsCancellationRequested) return; // Stop()중일 경우 Pass

                var today = DateTime.Today;
                if (_lastRunDate == today)
                {
                    var delay = GetDelayUntilNext();
                    _timer.Change(delay, Timeout.InfiniteTimeSpan);
                    return;
                }
                _lastRunDate = today;

                _action(_cts.Token);

                // 취소 시 아래 스케줄링 필요 없음
                _cts.Token.ThrowIfCancellationRequested();

                // 다음 스케줄 계산 (자정 + offset)
                var nextDelay = GetDelayUntilNext();

                _timer.Change(nextDelay, Timeout.InfiniteTimeSpan);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { TimerExceptionEvent?.Invoke(this, ex); }
            finally { _ = Interlocked.Exchange(ref _running, 0); }
        }

        private TimeSpan GetDelayUntilNext()
        {
            DateTime now = DateTime.Now;
            return now.Date.AddDays(1).Add(MidnightOffset) - now; // 자정 + offset
        }

        public void Stop()
        {
            _cts?.Cancel();
            var t = Interlocked.Exchange(ref _timer, null);
            t?.Dispose();
            _cts?.Dispose();
            _cts = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
