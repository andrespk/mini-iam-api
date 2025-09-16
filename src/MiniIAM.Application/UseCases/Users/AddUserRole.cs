using FluentValidation;
using Mapster;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Handlers;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Application.UseCases.Users;

public static class AddUserUser
{
    public sealed record Command(Guid UserId, IList<RoleDto> Roles, Guid ByUserId)
        : ICommand<IHandlerResponse<Guid>>, ICommand;

    public sealed class Validator : Validator<Command>
    {
        public Validator(IUserReadRepository repository, IRoleReadRepository roleRepository)
        {
            RuleFor(x => x.UserId)
                .NotNull()
                .MustAsync(async (id, ct) =>
                    (await repository.GetByIdAsync(id, ct)).Data is null)
                .WithMessage("Invalid User ID.");

            RuleFor(x => x.ByUserId)
                .NotNull()
                .MustAsync(async (id, ct) =>
                    (await repository.GetByIdAsync(id, ct)).Data is null)
                .WithMessage("Invalid User ID.");
            
            RuleFor(x => x.Roles)
                .NotNull()
                .MustAsync((roles, ct) =>
                   Task.FromResult(roles.All(role => roleRepository.GetById(role.Id).Data != null)))
                .WithMessage("Invalid User ID.");
        }
    }

    public sealed class Handler : CommandHandler<Command, Guid>
    {
        private readonly IHandlerContext _context;
        private readonly IUserWriteRepository _repository;
        private readonly Validator _validator;

        public Handler(IHandlerContext context, IUserWriteRepository repository,
            IUserReadRepository readRepository, IRoleReadRepository roleRepository) : base(context)
        {
            _repository = repository;
            _context = context;
            _validator = new Validator(readRepository, roleRepository);
        }

        public override async Task<IHandlerResponse<Guid>> ExecuteAsync(Command command, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var validation = await _validator.ValidateAsync(command, ct);
                if (validation.IsValid)
                {
                    var user = command.Adapt<User>();
                    var result = await _repository.AddRolesAsync(command.UserId, command.Roles, command.ByUserId, ct);

                    if (result.IsSuccess) return Success(user.Id);
                    return Error(result.Notifications.GetStringfiedList());
                }

                return Error(string.Join("\n", validation.Errors.Select(x => x.ErrorMessage)));
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