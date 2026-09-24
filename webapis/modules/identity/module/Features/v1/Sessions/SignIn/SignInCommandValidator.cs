using FluentValidation;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.SignIn;

public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.ReturnUrl)
            .Must(beLocalUrl!)
            .When(static command => command.ReturnUrl is not null)
            .WithMessage("The return URL must be a local path.");
    }

    // Same rule as ASP.NET's IUrlHelper.IsLocalUrl: a rooted path, not "//host" or "/\host", and no control characters, which browsers strip ("/\t/host" becomes "//host").
    private static bool beLocalUrl(string url)
        => url.StartsWith('/')
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            && !url.Any(char.IsControl);
}
