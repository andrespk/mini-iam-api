namespace MiniIAM.Infrastructure.Data;

public sealed record Notification(string Level, string Message, object? Details = null)
{
    public Ulid Id { get; } = Ulid.NewUlid();

    public static Notification NewInfo(string message, object? details = null) =>
        new(NotificationLevel.Info, message, details);

    public static Notification NewError(string message, object? details = null) =>
        new(NotificationLevel.Error, message, details);

    public static Notification NewError(Exception exception) =>
        new(NotificationLevel.Error, exception.Message, exception);

    public static Notification NewWarning(string message, object? details = null) =>
        new(NotificationLevel.Warning, message, details);

    public static Notification NewWarning(Exception exception) =>
        new(NotificationLevel.Warning, exception.Message, exception);
};