using System;
using System.Collections.Generic;

namespace VRCGoWorld.Video
{
    /// <summary>
    /// Deterministic queue state container for synchronized video playback.
    /// Designed to be portable into UdonSharp with minimal changes.
    /// </summary>
    public class VideoQueueState
    {
        public const int DefaultMaxQueueItems = 32;
        public const int MaxUrlLength = 2048;
        public const int MaxDisplayNameLength = 64;

        private readonly List<VideoQueueItem> _queue;

        public int MaxQueueItems { get; }
        public IReadOnlyList<VideoQueueItem> Queue => _queue;
        public int Count => _queue.Count;
        public bool HasCurrent => _queue.Count > 0;
        public VideoQueueItem Current => HasCurrent ? _queue[0] : default;

        public VideoQueueState(int maxQueueItems = DefaultMaxQueueItems)
        {
            MaxQueueItems = maxQueueItems > 0 ? maxQueueItems : DefaultMaxQueueItems;
            _queue = new List<VideoQueueItem>(MaxQueueItems);
        }

        public QueueActionResult TryEnqueue(string url, string addedBy, long addedAtUnixMs)
        {
            if (_queue.Count >= MaxQueueItems)
            {
                return QueueActionResult.QueueFull;
            }

            var normalizedUrl = NormalizeUrl(url);
            if (normalizedUrl == null)
            {
                return QueueActionResult.InvalidUrl;
            }

            var normalizedAddedBy = NormalizeDisplayName(addedBy);

            _queue.Add(new VideoQueueItem
            {
                Url = normalizedUrl,
                AddedBy = normalizedAddedBy,
                AddedAtUnixMs = addedAtUnixMs
            });

            return QueueActionResult.Accepted;
        }

        /// <summary>
        /// Removes current video and advances to next queued item if present.
        /// </summary>
        public QueueActionResult TrySkipCurrent()
        {
            if (!HasCurrent)
            {
                return QueueActionResult.EmptyQueue;
            }

            _queue.RemoveAt(0);
            return QueueActionResult.Accepted;
        }

        public QueueActionResult TryRemoveAt(int index)
        {
            if (index < 0 || index >= _queue.Count)
            {
                return QueueActionResult.OutOfRange;
            }

            _queue.RemoveAt(index);
            return QueueActionResult.Accepted;
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public QueueSnapshot Snapshot()
        {
            return new QueueSnapshot
            {
                QueueItems = _queue.ToArray()
            };
        }

        public QueueActionResult Restore(QueueSnapshot snapshot)
        {
            _queue.Clear();

            if (snapshot.QueueItems == null || snapshot.QueueItems.Length == 0)
            {
                return QueueActionResult.Accepted;
            }

            var max = Math.Min(snapshot.QueueItems.Length, MaxQueueItems);
            for (var i = 0; i < max; i++)
            {
                var item = snapshot.QueueItems[i];
                var normalizedUrl = NormalizeUrl(item.Url);
                if (normalizedUrl == null)
                {
                    continue;
                }

                _queue.Add(new VideoQueueItem
                {
                    Url = normalizedUrl,
                    AddedBy = NormalizeDisplayName(item.AddedBy),
                    AddedAtUnixMs = item.AddedAtUnixMs
                });
            }

            return QueueActionResult.Accepted;
        }

        private static string NormalizeUrl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var trimmed = raw.Trim();
            if (trimmed.Length > MaxUrlLength)
            {
                trimmed = trimmed.Substring(0, MaxUrlLength);
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

            return uri.ToString();
        }

        private static string NormalizeDisplayName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Unknown";
            var trimmed = raw.Trim();
            if (trimmed.Length > MaxDisplayNameLength)
            {
                trimmed = trimmed.Substring(0, MaxDisplayNameLength);
            }

            return trimmed;
        }
    }

    public enum QueueActionResult
    {
        Accepted = 0,
        InvalidUrl = 1,
        QueueFull = 2,
        EmptyQueue = 3,
        OutOfRange = 4,
        RateLimited = 5,
        Unauthorized = 6
    }

    public struct VideoQueueItem
    {
        public string Url;
        public string AddedBy;
        public long AddedAtUnixMs;
    }

    public struct QueueSnapshot
    {
        public VideoQueueItem[] QueueItems;
    }
}
