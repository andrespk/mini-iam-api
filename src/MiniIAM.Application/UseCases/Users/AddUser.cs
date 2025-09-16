using FluentValidation;
using Mapster;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Handlers;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Application.UseCases.Users;

public static class AddUser
{
    public sealed record Command(string Email, string Name, string Password, Guid byUserId)
        : ICommand<IHandlerResponse<Guid>>;

    public sealed class Validator : Validator<Command>
    {
        public Validator(IUserReadRepository repository)
        {
            RuleFor(x => x.Email)
                .NotNull()
                .NotEmpty()
                .EmailAddress()
                .MustAsync(async (id, ct) =>
                    (await repository.GetByEmailAsync(id, ct)).Data is null)
                .WithMessage("Invalid or unregistered e-mail.");

            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .WithMessage("Invalid name.");

            RuleFor(x => x.Name.Length)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100)
                .WithMessage("Invalid name.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Invalid password.");

            RuleFor(x => x.Password.Length)
                .GreaterThanOrEqualTo(6)
                .WithMessage("Invalid password.");
        }
    }

    public sealed class Handler : CommandHandler<Command, Guid>
    {
        private readonly IHandlerContext _context;
        private readonly IUserWriteRepository _repository;
        private readonly Validator _validator;

        public Handler(IHandlerContext context, IUserWriteRepository repository,
            IUserReadRepository readRepository) : base(context)
        {
            _repository = repository;
            _context = context;
            _validator = new Validator(readRepository);
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