using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Handlers;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Application.UseCases.Users;

public static class GetUser
{
    public sealed record Command(Guid Id) : ICommand<IHandlerResponse<UserDto>>, ICommand, ICommand<UserDto>;

    public sealed class Handler : CommandHandler<Command, UserDto>
    {
        private readonly IHandlerContext _context;
        private readonly IUserReadRepository _repository;

        public Handler(IHandlerContext context, IUserReadRepository repository) : base(context)
        {
            _context = context;
            _repository = repository;
        }

        public override async Task<IHandlerResponse<UserDto>> ExecuteAsync(Command command, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var result = await _repository.GetByIdAsync(command.Id, ct);

                if (result.IsSuccess)
                    return Success(result.Data!);

                return Error(result.Notifications.GetStringfiedList());
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                _context.Logger.Error(errorMessage, ex);
                return Error(errorMessage);
            }
        }
    }
}