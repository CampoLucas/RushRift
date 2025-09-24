using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Utility class that ensures that a part of the code is not executed more often than a given interval.
    /// Can be paused and resumed without losing track of elapsed time.
    /// </summary>
    public class RateLimiter
    {
        private readonly float _interval; // Minimum time that must pass between calls.
        private readonly bool _useUnscaledTime; // Whether to use Time.unscaledTime (ignores timescale) or Time.time.
        private float _lastCallTime; // The last time a call was allowed.
        private float _pausedStartTime; // Time when Pause() was called
        private bool _paused; // Whether the limiter is currently paused

        /// <param name="intervalSeconds">How many seconds must pass between allowed calls.</param>
        /// <param name="startPaused">Whether the limiter starts in a paused state.</param>
        /// <param name="useUnscaledTime">Use Time.unscaledTime instead of Time.time.</param>
        /// <exception cref="ArgumentException">Argument thrown because the interval is zero or lower.</exception>
        public RateLimiter(float intervalSeconds = .1f, bool startPaused = false, bool useUnscaledTime = false)
        {
            if (intervalSeconds <= 0)
                throw new ArgumentException("Interval must be greater than zero.", nameof(intervalSeconds));

            _interval = intervalSeconds;
            _useUnscaledTime = useUnscaledTime;

            var time = GetTime();
            _lastCallTime = time;
            _paused = startPaused;
            
            if (startPaused) _pausedStartTime = time;
        }

        /// <summary>
        /// Checks if enough time has passed to allow a call.
        /// 
        /// </summary>
        /// <param name="delta">The delta time between calls</param>
        /// <returns>
        /// If true, updates internal state and returns the elapsed delta.
        /// If false, delta is zero and no update is allowed.
        /// </returns>
        public bool CanCall(out float delta)
        {
            delta = 0f;
            if (_paused) return false;
            
            var time = GetTime();
            delta = time - _lastCallTime;
            
            if (delta >= _interval)
            {
                _lastCallTime = time;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Pauses the limiter so no calls can pass through.
        /// Records the time at which the pause started.
        /// </summary>
        public void Pause()
        {
            if (_paused) return;
            _paused = true;
            _pausedStartTime = GetTime();
        }

        /// <summary>
        /// Resumes the limiter, shifting the internal baseline forward
        /// by the duration of the pause so that elapsed delta does not jump.
        /// </summary>
        public void Resume()
        {
            if (!_paused) return;
            _paused = false;

            var pausedDuration = GetTime() - _pausedStartTime;
            _lastCallTime += pausedDuration;
        }

        /// <summary>
        /// Returns either Time.time or Time.unscaledTime depending on configuration.
        /// </summary>
        private float GetTime() => _useUnscaledTime ? Time.unscaledTime : Time.time;
    }
}