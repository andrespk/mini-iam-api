using MiniIAM.Infrastructure.Data;

namespace MiniIAM.Infrastructure.Extensions;

public static class ExceptionExtensions
{
    public static Notification ToNotification(this Exception exception)
        => new(NotificationLevel.Error, exception.Message,
            new
            {
                Failure = exception.GetType().Name,
                exception.Source,
                exception.StackTrace,
                InnerFailure = exception.InnerException?.GetType().Name,
                InnerFailureSource = exception.InnerException?.Source,
                InnerFailureMessage = exception.InnerException?.Message,
                InnerFailureStackTrace = exception.InnerException?.StackTrace
            });
}