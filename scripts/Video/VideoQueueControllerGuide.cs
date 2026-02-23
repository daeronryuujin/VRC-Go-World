using System;

namespace VRCGoWorld.Video
{
    /// <summary>
    /// Controller-style scaffold that can be translated to UdonSharp behavior methods.
    /// Uses explicit result codes so UI can show clear feedback.
    /// </summary>
    public class VideoQueueControllerGuide
    {
        private readonly VideoQueueState _state = new VideoQueueState();
        private readonly RateLimiter _queueLimiter = new RateLimiter();
        private readonly RateLimiter _skipLimiter = new RateLimiter();

        public int QueueCooldownMs { get; set; } = 8_000;
        public int SkipCooldownMs { get; set; } = 4_000;

        /// <summary>
        /// Set true if everyone can skip. Set false for owner/mod flow.
        /// </summary>
        public bool AllowAllUsersToSkip { get; set; } = false;

        public QueueActionResult RequestEnqueue(string userId, string url, string displayName, long nowUnixMs)
        {
            if (string.IsNullOrWhiteSpace(userId)) return QueueActionResult.Unauthorized;

            if (!_queueLimiter.TryAcquire($"enqueue:{userId}", nowUnixMs, QueueCooldownMs))
            {
                return QueueActionResult.RateLimited;
            }

            return _state.TryEnqueue(url, displayName, nowUnixMs);
        }

        public QueueActionResult RequestSkip(string userId, bool isPrivilegedUser, long nowUnixMs)
        {
            if (string.IsNullOrWhiteSpace(userId)) return QueueActionResult.Unauthorized;
            if (!AllowAllUsersToSkip && !isPrivilegedUser) return QueueActionResult.Unauthorized;

            if (!_skipLimiter.TryAcquire($"skip:{userId}", nowUnixMs, SkipCooldownMs))
            {
                return QueueActionResult.RateLimited;
            }

            return _state.TrySkipCurrent();
        }

        public QueueActionResult RequestRemoveAt(string userId, bool isPrivilegedUser, int index)
        {
            if (string.IsNullOrWhiteSpace(userId)) return QueueActionResult.Unauthorized;
            if (!isPrivilegedUser) return QueueActionResult.Unauthorized;

            return _state.TryRemoveAt(index);
        }

        public void RequestClear(string userId, bool isPrivilegedUser)
        {
            if (string.IsNullOrWhiteSpace(userId) || !isPrivilegedUser) return;
            _state.Clear();
            _queueLimiter.ResetAll();
            _skipLimiter.ResetAll();
        }

        public QueueSnapshot BuildSnapshotForSync()
        {
            return _state.Snapshot();
        }

        public QueueActionResult RestoreFromSync(QueueSnapshot snapshot)
        {
            return _state.Restore(snapshot);
        }

        public string GetNowPlayingUrlOrNull()
        {
            return _state.HasCurrent ? _state.Current.Url : null;
        }

        public string[] BuildQueueDisplayLines()
        {
            var lines = new string[_state.Count];

            for (var i = 0; i < _state.Count; i++)
            {
                var item = _state.Queue[i];
                var prefix = i == 0 ? "▶ " : $"{i}. ";
                lines[i] = $"{prefix}{item.AddedBy}: {item.Url}";
            }

            return lines;
        }
    }
}
