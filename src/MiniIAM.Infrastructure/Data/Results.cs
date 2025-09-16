using MiniIAM.Infrastructure.Data.Paging;

namespace MiniIAM.Infrastructure.Data;

public class ResultBase<T>(T? data = default, IList<Notification>? notifications = null, PageMeta? pageMeta = null)
{
    public T? Data { get; private set; } = data;
    public PageMeta? PageMeta { get; private set; } = pageMeta;
    public NotificationStack Notifications { get; private set; } = new(notifications);
    public bool IsSuccess => Notifications.List.All(n => n.Level != NotificationLevel.Error);

    public void RefreshListWith(IList<Notification> notifications) =>
        Notifications = new NotificationStack(notifications);

    public void SetData(T data) => Data = data!;

    public void SetPageMeta(PageMeta pageMeta) => PageMeta = pageMeta;

    public void AddNotification(Notification notification) => Notifications.Add(notification);

    public void RemoveNotification(Ulid notificationId) => Notifications.Remove(notificationId);
}

public class ResultListBase<T>(
    IList<T>? data = null,
    IList<Notification>? notifications = null,
    PageMeta? pageMeta = null)
{
    public IList<T>? Data { get; private set; } = data;
    public PageMeta? PageMeta { get; private set; } = pageMeta;
    public NotificationStack Notifications { get; private set; } = new(notifications);

    public bool IsSuccess =>
        Notifications.List.Count == 0 || Notifications.List.All(n => n.Level != NotificationLevel.Error);

    public void RefreshListWith(IList<Notification> notifications) =>
        Notifications = new NotificationStack(notifications);

    public void SetData(IList<T>? data)
    {
        if (data != null) Data = data;
    }

    public void SetPageMeta(PageMeta pageMeta) => PageMeta = pageMeta;

    public void AddNotification(Notification notification) => Notifications.Add(notification);

    public void RemoveNotification(Ulid notificationId) => Notifications.Remove(notificationId);
}

public sealed class Result<T>(T? data, IList<Notification>? notifications = null)
    : ResultBase<T>(data, notifications)
{
    private Result(string message, T? data, IList<Notification>? notifications = null,
        PageMeta? pageMeta = null) : this(data, notifications)
    {
        if (notifications != null) RefreshListWith(notifications);
        if (data != null) SetData(data);
        if (!string.IsNullOrEmpty(message)) Notifications.Add(Notification.NewInfo(message));
    }

    public static Result<T> Success() => new(string.Empty, default);

    public static Result<T> Success(T resultContent) => new(resultContent);

    public static Result<T> Success(string message, T resultContent) => new(message, resultContent);

    public static Result<T> Failure(string error, object? errorDetails = null) =>
        new(default, new List<Notification> { Notification.NewError(error, errorDetails) });

    public static Result<T> Failure(Exception exception) =>
        new(default, new List<Notification> { Notification.NewError(exception) });

    public static Result<T> Failure(IList<string> errors, object? errorDetails = null) =>
        errors.Select(x => new Notification(NotificationLevel.Error, x, errorDetails)).ToList() is var
            notifications
            ? new Result<T>(default, notifications)
            : new Result<T>(default, new List<Notification>());

    public static implicit operator Result<T>(Result result) => new Result<T>(default, result.Notifications.List);

    public static implicit operator Result(Result<T> result) => new Result(result.Notifications.List);
}

public sealed class ResultList<T>(IList<T>? data, IList<Notification>? notifications = null, PageMeta? pageMeta = null)
    : ResultListBase<T>(data, notifications, pageMeta)
{
    private ResultList(string message, IList<T>? data, IList<Notification>? notifications = null,
        PageMeta? pageMeta = null) : this(data, notifications, pageMeta)
    {
        if (notifications != null) RefreshListWith(notifications);
        if (!string.IsNullOrEmpty(message)) Notifications.Add(Notification.NewInfo(message));
        SetData(data);
    }

    public static ResultList<T> Success(IList<T> resultContent, PageMeta? pageMeta) =>
        new(resultContent, default, pageMeta);

    public static ResultList<T> Success(PageMeta pageMeta) => new(default, default, pageMeta);

    public static ResultList<T> Success(string message, IList<T> resultContent)
    {
        return new ResultList<T>(message, resultContent);
    }

    public static ResultList<T> Failure(string error, object? errorDetails = null) => new(default,
        new List<Notification> { Notification.NewError(error, errorDetails) });

    public static ResultList<T> Failure(Exception exception) =>
        new(default, new List<Notification> { Notification.NewError(exception) });

    public static ResultList<T> Failure(IList<string> errors, object? errorDetails = null)
    {
        return errors.Select(x => new Notification(NotificationLevel.Error, x, errorDetails)).ToList() is var
            notifications
            ? new ResultList<T>(default, notifications)
            : new ResultList<T>(default, new List<Notification>());
    }

    public static implicit operator ResultList<T>(Result result) => new ResultList<T>(default, result.Notifications.List);

    public static implicit operator Result(ResultList<T> result) => new Result(result.Notifications.List);
}

public sealed class Result(IList<Notification>? notifications = null)
    : ResultBase<object>(notifications)
{
    private Result(string message, IList<Notification>? notifications = null) : this(notifications)
    {
        if (notifications != null) RefreshListWith(notifications);
        if (!string.IsNullOrEmpty(message)) Notifications.Add(Notification.NewInfo(message));
    }

    public static Result Success() => new();

    public static Result Success(string message)
    {
        var messageNotification = new List<Notification> { Notification.NewInfo(message) };   
        return new Result(messageNotification);
    }

    public static Result Failure(string error, object? errorDetails = null) =>
        new(default, new List<Notification> { Notification.NewError(error, errorDetails) });

    public static Result Failure(Exception exception) =>
        new(new List<Notification> { Notification.NewError(exception) });

    public static Result Failure(IList<string> errors, object? errorDetails = null) =>
        errors.Select(x => new Notification(NotificationLevel.Error, x, errorDetails)).ToList() is var
            notifications
            ? new Result(notifications)
            : new Result(new List<Notification>());

    public static Result NoDataFound(object? errorDetails = null) =>
        new ("No data found.", new List<Notification>());
}