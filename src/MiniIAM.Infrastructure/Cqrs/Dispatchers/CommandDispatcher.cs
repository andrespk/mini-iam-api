using Microsoft.Extensions.DependencyInjection;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Infrastructure.Cqrs.Dispatchers;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    public CommandDispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public void Dispatch<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        handler.Handle(command);
    }

    public TResponse Dispatch<TCommand, TResponse>(TCommand command) where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return handler.Handle(command);
    }

    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        await handler.HandleAsync(command, ct);
    }

    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.HandleAsync(command, ct);
    }
}