using System.Text;
using System.Text.Json;
using FluentValidation;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Domain;

namespace yggdrasil.Modules.Profiles.Features.v1.Profiles.ReplaceProfileData;

public sealed class ReplaceProfileDataCommandValidator : AbstractValidator<ReplaceProfileDataCommand>
{
    public ReplaceProfileDataCommandValidator()
    {
        RuleFor(static command => command.Data)
            .Must(static data => data.ValueKind == JsonValueKind.Object)
            .WithMessage("Profile data msut be a JSON object.")
            .Must(static data => Encoding.UTF8.GetByteCount(data.GetRawText()) <= GameProfile.DATA_MAX_BYTES)
            .WithMessage($"Profile data must not exceed {GameProfile.DATA_MAX_BYTES / 1024} KiB");
    }
}
