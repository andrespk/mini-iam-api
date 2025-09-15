using MinimalCqrs;
using FluentValidation;
using MiniIAM.Infrastructure.Auth.Abstractions;
using MiniIAM.Infrastructure.Handlers.Abstractions;

public static class LogOutUser
{
    public sealed record Command(string AccessToken)
        : ICommand<IHandlerResponse<DateTime>>;

    public sealed class Validator : Validator<Command>
    {
        public Validator(IAuthService authService)
        {
            RuleFor(x => x.AccessToken)
                .NotNull()
                .NotEmpty()
                .Must(x => authService.IsJwtValid(x).Data)
                .WithMessage("Invalid Access Token.");
        }
    }

    public sealed class Handler : CommandHandler<Command, DateTime>
    {
        private readonly IHandlerContext _context;
        private readonly IAuthService _authService;
        private readonly Validator _validator;
        public Handler(IHandlerContext context, IAuthService authService) : base(context)
        {
            _authService = authService;
            _context = context;
            _validator = new Validator(_authService);
        }

        public override async Task<IHandlerResponse<DateTime>> ExecuteAsync(Command command, CancellationToken ct = default)
        {
            try
            {
                var validation = await _validator.ValidateAsync(command, ct);
                if (validation.IsValid)
                {
                    var result = await _authService.LogOutAsync(command.AccessToken);
                    return result.IsSuccess? Success(DateTime.UtcNow) : Error(result.Notifications.GetStringfiedList()); 
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