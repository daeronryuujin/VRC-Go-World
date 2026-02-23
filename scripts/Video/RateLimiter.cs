using System.Collections.Generic;

namespace VRCGoWorld.Video
{
    /// <summary>
    /// Per-key cooldown tracker for queue actions.
    /// </summary>
    public class RateLimiter
    {
        private readonly Dictionary<string, long> _lastSeenByKey = new Dictionary<string, long>();

        public bool TryAcquire(string key, long nowUnixMs, int cooldownMs)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (cooldownMs < 0) cooldownMs = 0;

            if (_lastSeenByKey.TryGetValue(key, out var last) && nowUnixMs - last < cooldownMs)
            {
                return false;
            }

            _lastSeenByKey[key] = nowUnixMs;
            return true;
        }

        public long GetRemainingMs(string key, long nowUnixMs, int cooldownMs)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            if (cooldownMs <= 0) return 0;
            if (!_lastSeenByKey.TryGetValue(key, out var last)) return 0;

            var elapsed = nowUnixMs - last;
            var remaining = cooldownMs - elapsed;
            return remaining > 0 ? remaining : 0;
        }

        public void Reset(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _lastSeenByKey.Remove(key);
        }

        public void ResetAll()
        {
            _lastSeenByKey.Clear();
        }
    }
}
