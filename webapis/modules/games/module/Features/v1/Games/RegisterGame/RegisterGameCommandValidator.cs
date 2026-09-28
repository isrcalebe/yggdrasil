using FluentValidation;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Games.Domain;

namespace yggdrasil.Modules.Games.Features.v1.Games.RegisterGame;

public sealed class RegisterGameCommandValidator : AbstractValidator<RegisterGameCommand>
{
    public RegisterGameCommandValidator()
    {
        RuleFor(static command => command.Slug)
            .NotEmpty()
            .MaximumLength(Game.SLUG_MAX_LENGTH)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Use lowercase letters, digits and single hyphens (e.g. 'my-game').");

        RuleFor(static command => command.Name)
            .NotEmpty()
            .MaximumLength(Game.NAME_MAX_LENGTH);
    }
}
