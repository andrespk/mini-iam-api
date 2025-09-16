using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Abstractions;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    void Handle(TCommand command);
    Task HandleAsync(TCommand command, CancellationToken token = default);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    TResponse Handle(TCommand command);
    Task<TResponse> HandleAsync(TCommand command, CancellationToken token = default);
}
