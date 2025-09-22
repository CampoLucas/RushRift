using System;
using UnityEngine;

namespace Game.DesignPatterns.Observers
{
    /// <summary>
    /// A Subject that broadcasts "ticks" (float deltas) to its observers.
    /// </summary>
    public class TickSubject : ISubject<float>
    {
        private RateLimiter _limiter; // Controls how often observers can be notified
        private Subject<float> _subject; // Underlying Subject that manages observers
        private bool _disposed; // Tracks whether this instance has been disposed

        /// <param name="intervalSeconds">How many seconds must pass between allowed calls.</param>
        /// <param name="startPaused">Whether the limiter starts in a paused state.</param>
        /// <param name="useUnscaledTime">Use Time.unscaledTime instead of Time.time.</param>
        /// <exception cref="ArgumentException">Argument thrown because the interval is zero or lower.</exception>
        public TickSubject(float intervalSeconds = .1f, bool startPaused = false, bool useUnscaledTime = false)
        {
            if (intervalSeconds <= 0)
                throw new ArgumentException("Interval must be greater than zero.", nameof(intervalSeconds));

            _subject = new Subject<float>(false, true);
            _limiter = new RateLimiter(intervalSeconds, startPaused, useUnscaledTime);
        }

        /// <summary>
        /// Must be called every frame (e.g., from Update()).
        /// Checks the limiter and notifies observers if the interval has passed.
        /// </summary>
        public void UpdateTick()
        {
            if (!_limiter.CanCall(out var delta)) return;
            _subject.NotifyAll(delta);
        }

        /// <summary>
        /// Pauses the ticking.
        /// </summary>
        public void Pause()
        {
            if (_disposed) return;
            _limiter.Pause();
        }

        /// <summary>
        /// Resumes ticking after a pause.
        /// </summary>
        public void Resume()
        {
            if (_disposed) return;
            _limiter.Resume();
        }
        
        /// <summary>
        /// Subscribes an observer to receive tick notifications.
        /// </summary>
        public bool Attach(IObserver<float> observer) => !_disposed && _subject.Attach(observer);

        /// <summary>
        /// Unsubscribes an observer.
        /// </summary>
        public bool Detach(IObserver<float> observer) => !_disposed && _subject.Detach(observer);

        /// <summary>
        /// Removes all observers.
        /// </summary>
        public void DetachAll()
        {
            if (_disposed) return;
            _subject.DetachAll();
        }

        /// <summary>
        /// Doesn't do anything in this case.
        /// </summary>
        /// <param name="arg"></param>
        [Obsolete("Required by ISubject<float> but unused here. Tick notifications should only be sent through UpdateTick().")]
        public void NotifyAll(float arg) { }
        
        /// <summary>
        /// Cleans up all observers and marks this subject as disposed.
        /// After disposal, no further updates or subscriptions are allowed.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _subject.DetachAll();
            _subject.Dispose();

            _subject = null;
            _limiter = null;
        }
    }
}