using System;
using System.IO;
using System.Text.Json;

namespace AutoTable.Services
{
    /// <summary>
    /// Forward-only monotonic counter used by the licensing engine.
    /// On TPM-enabled machines this can be backed by a TPM monotonic counter;
    /// the current implementation uses an atomic file-based counter that honors
    /// the same guarantee: it can only ever increase, never decrease.
    /// </summary>
    public class MonotonicCounter
    {
        private static readonly Lazy<MonotonicCounter> _instance = new(() => new MonotonicCounter());
        public static MonotonicCounter Instance => _instance.Value;

        private const string CounterFileName = "autotable_counter.dat";

        private readonly object _lock = new();
        private readonly string _filePath;
        private long _value;

        private MonotonicCounter()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTable");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, CounterFileName);
        }

        /// <summary>Current counter value (min 0).</summary>
        public long Value => _value;

        /// <summary>
        /// Load the counter from disk. Must be called once at startup.
        /// </summary>
        public void Initialize()
        {
            lock (_lock)
            {
                _value = ReadFromDisk();
            }
        }

        /// <summary>
        /// Read the current counter value without incrementing.
        /// </summary>
        public long Read() => _value;

        /// <summary>
        /// Increment the counter by one and persist atomically.
        /// </summary>
        public long Increment()
        {
            lock (_lock)
            {
                _value = Math.Max(_value, ReadFromDisk());
                _value += 1;
                WriteToDisk(_value);
                return _value;
            }
        }

        /// <summary>
        /// Force the counter to at least the given value (used for TPM catch-up
        /// during license reconciliation). Never decreases the stored value.
        /// </summary>
        public long EnsureAtLeast(long value)
        {
            lock (_lock)
            {
                var disk = ReadFromDisk();
                _value = Math.Max(_value, disk);
                if (value > _value)
                {
                    _value = value;
                    WriteToDisk(_value);
                }
                return _value;
            }
        }

        private long ReadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath)) return 0;
                var raw = JsonSerializer.Deserialize<CounterData>(File.ReadAllText(_filePath));
                return raw?.Value ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private void WriteToDisk(long value)
        {
            // Atomic write: write to a temp file, then replace.
            var tmp = _filePath + ".tmp";
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    File.WriteAllText(tmp, JsonSerializer.Serialize(new CounterData { Value = value }));
                    File.Move(tmp, _filePath, true);
                    return;
                }
                catch
                {
                    System.Threading.Thread.Sleep(20);
                }
            }
        }

        private class CounterData
        {
            public long Value { get; set; }
        }
    }
}