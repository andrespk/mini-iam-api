using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Abstractions;

public interface ICommandDispatcher
{
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand;

    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResponse>;
}