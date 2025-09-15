using MinimalCqrs;
using FluentValidation;
using Mapster;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MiniIAM.Infrastructure.Handlers.Abstractions;

public static class UpdateUser
{
    public sealed record Command(string Name, IEnumerable<RoleDto> Roles, Guid byUserId)
        : ICommand<IHandlerResponse<Guid>>;

    public sealed class Validator : Validator<Command>
    {
        public Validator(IRoleReadRepository repository)
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .WithMessage("Invalid name.");

            RuleFor(x => x.Name.Length)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100)
                .WithMessage("Invalid name.");

            RuleFor(x => x.Roles)
                .NotNull()
                .Must((cmd, roles) => roles.All(x => repository.GetById(x.Id).Data != null))
                .WithMessage("Invalid or unregistered e-mail.");
        }
    }

    public sealed class Handler : CommandHandler<Command, Guid>
    {
        private readonly IHandlerContext _context;
        private readonly IUserWriteRepository _repository;
        private readonly Validator _validator;

        public Handler(IHandlerContext context, IUserWriteRepository repository,
            IRoleReadRepository roleRepository) : base(context)
        {
            _repository = repository;
            _context = context;
            _validator = new Validator(roleRepository);
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
                    var result = await _repository.InsertAsync(user, command.byUserId, ct);

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