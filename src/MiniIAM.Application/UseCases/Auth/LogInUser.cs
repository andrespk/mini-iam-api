using MinimalCqrs;
using FluentValidation;
using Mapster;
using MiniIAM.Infrastructure.Auth.Abstractions;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Handlers.Abstractions;

public static class LogInUser
{
    public sealed record Command(string Email, string Password)
        : ICommand<IHandlerResponse<Response>>;

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
                .WithMessage("Invalid password.");
            
            RuleFor(x => x.Password.Length)
                .GreaterThanOrEqualTo(6)
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

        public override async Task<IHandlerResponse<Response>> ExecuteAsync(Command command, CancellationToken ct = default)
        {
           try
            {
                var validation = await _validator.ValidateAsync(command, ct);
                if (validation.IsValid)
                {
                    var result = await _service.LogInAsync(command.Adapt<LoginRequestDto>());
                    
                    if(result.IsSuccess)
                        return Success(result.Data.Adapt<Response>());
                    
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