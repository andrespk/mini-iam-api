using FluentValidation;
using Mapster;
using MiniIAM.Infrastructure.Auth.Abstractions;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Handlers;
using MinimalCqrs;

namespace MiniIAM.Application.UseCases.Auth;

public static class LogInUser
{
    public sealed record Command(string Email, string Password, bool? IsFirstAccess = null) : ICommand, ICommand<Response>;

    public sealed record Response(bool IsLoggedIn, string AccessToken, string RefreshToken);

    public sealed class Validator : Validator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Invalid e-mail.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Invalid password.")
                .Length(6, 100)
                .WithMessage("Invalid password.");
        }
    }

    public sealed class Handler : CommandHandler<Command, Response>
    {
        private readonly IHandlerContext _context;
        private readonly IAuthService _service;
        private readonly Validator _validator;
        public Handler(IHandlerContext context, IAuthService service) : base(context)
        {
            _service = service;
            _context = context;
            _validator = new Validator();
        }

        public override async Task<Response> HandleAsync(Command command, CancellationToken ct = default)
        {
            try
            {
                var validation = await _validator.ValidateAsync(command, ct);
                if (!validation.IsValid)
                {
                    var errorMessage = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
                    _context.Logger.Warning("Validation failed: {ErrorMessage}", errorMessage);
                    return new Response(false, string.Empty, string.Empty);
                }

                var result = await _service.LogInAsync(command.Adapt<LoginRequestDto>());
                
                if(result.IsSuccess)
                    return result.Data.Adapt<Response>();
                
                var serviceErrorMessage = result.Notifications.GetStringfiedList();
                _context.Logger.Warning("Login failed: {ErrorMessage}", serviceErrorMessage);
                return new Response(false, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                _context.Logger.Error(errorMessage, ex);
                return new Response(false, string.Empty, string.Empty);
            }
        }

        public override Response Handle(Command command)
        {
            return HandleAsync(command).GetAwaiter().GetResult();
        }
    }
}