using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AutoTable.Services
{
    /// <summary>
    /// Light in-memory history of the most recent notifications so the
    /// shell's Notifications panel can display them even after the on-
    /// screen toast has been dismissed.
    /// </summary>
    public sealed class StoredNotification
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
        public bool IsRead { get; set; }
    }

    public sealed class NotificationStore
    {
        private readonly List<StoredNotification> _items = new();
        public IReadOnlyList<StoredNotification> Items => _items;
        public event Action? Changed;

        public void Add(StoredNotification n)
        {
            _items.Insert(0, n);
            if (_items.Count > 20) _items.RemoveAt(_items.Count - 1);
            NotifyChanged();
        }
        public void MarkAllRead() { foreach (var i in _items) i.IsRead = true; NotifyChanged(); }
        public void ClearAll() { _items.Clear(); NotifyChanged(); }
        public void NotifyChanged() => Changed?.Invoke();
    }

    /// <summary>
    /// Deliver notifications as Windows Toast notifications (bottom-right
    /// of the screen and Action Center) and keep a short in-memory history
    /// for the shell's Notifications panel. Uses the Windows SDK toast
    /// template API so no extra NuGet package is required.
    /// </summary>
    public sealed class ToastService
    {
        private static readonly Lazy<ToastService> _instance = new(() => new());
        public static ToastService Instance => _instance.Value;

        private readonly NotificationStore _store = new();
        public NotificationStore Store => _store;

        private ToastService() { }

        public async Task ShowAsync(string title, string message, string? tag = null)
        {
            // In-app history for the Notifications panel.
            _store.Add(new StoredNotification { Title = title, Message = message });
            _store.NotifyChanged();

            // Real Windows toast (bottom-right / Action Center).
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml($@"<toast duration='short'>
<visual><binding template='ToastGeneric'>
<text>{SecurityElement.Escape(title)}</text>
<text>{SecurityElement.Escape(message)}</text>
</binding></visual></toast>");

                var toast = new ToastNotification(doc);
                if (!string.IsNullOrEmpty(tag)) toast.Tag = tag;
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch
            {
                // Toasts are a convenience; never block the operation that produced them.
            }
        }

        public void Show(string title, string message, string? tag = null) =>
            _ = ShowAsync(title, message, tag);
    }
}

