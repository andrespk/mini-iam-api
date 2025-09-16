using MiniIAM.Infrastructure.Extensions;

namespace MiniIAM.Infrastructure.Data;

public sealed class NotificationStack
{
    private IList<Notification> _notifications = new List<Notification>();
    public IList<Notification> List => _notifications;

    public NotificationStack(IEnumerable<Notification>? notifications = null)
    {
        if (notifications != null)
            AddMany(notifications);
    }

    public void Add(Notification notification) => _notifications.Add(notification);

    public void AddMany(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
            Add(notification);
    }

    public void AddAsWarning(string message, Exception? exception = null) =>
        _notifications.Add(new Notification(NotificationLevel.Error, message, exception));

    public void AddAsFailure(Exception ex) => _notifications.Add(ex.ToNotification());

    public void AddAsFailure(string errorMessage) =>
        _notifications.Add(new Notification(NotificationLevel.Error, errorMessage));

    public void AddAsFailure(IEnumerable<string> errorMessages)
    {
        foreach (var errorMessage in errorMessages)
            AddAsFailure(errorMessage);
    }

    public void Remove(Ulid notificationId) =>
        _notifications = _notifications.Where(n => n.Id != notificationId).ToList();

    public string GetStringfiedList() => string.Join(",", _notifications.Select(x => $"{x.Level}: {x.Message}"));
}