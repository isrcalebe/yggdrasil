using FluentValidation;
using Mediator;

namespace yggdrasil.Web.Behaviors;

/// <summary>
/// Runs the FluentValidation validators of every message sent through the mediator, whether it comes from an
/// endpoint or from another module. Failures throw <see cref="ValidationException"/>, turned into a
/// <c>400 ValidationProblem</c> by <see cref="Exceptions.ValidationExceptionHandler"/>.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly IValidator<TMessage>[] materializedValidators = [.. validators];

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (materializedValidators.Length > 0)
        {
            var context = new ValidationContext<TMessage>(message);
            var failures = new List<FluentValidation.Results.ValidationFailure>();

            foreach (var validator in materializedValidators)
                failures.AddRange((await validator.ValidateAsync(context, cancellationToken)).Errors);

            if (failures.Count > 0)
                throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }
}
