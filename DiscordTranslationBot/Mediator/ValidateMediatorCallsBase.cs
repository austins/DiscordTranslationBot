using FluentValidation;
using FluentValidation.Results;

namespace DiscordTranslationBot.Mediator;

public abstract class ValidateMediatorCallsBase
{
    private readonly IServiceProvider _serviceProvider;

    protected ValidateMediatorCallsBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected async Task ValidateOrThrowAsync(object instanceToValidate, CancellationToken cancellationToken)
    {
        var validators = _serviceProvider
            .GetServices(typeof(IValidator<>).MakeGenericType(instanceToValidate.GetType()))
            .OfType<IValidator>()
            .ToList();

        if (validators.Count > 0)
        {
            var failures = new List<ValidationFailure>();
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(instanceToValidate),
                    cancellationToken
                );

                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }
    }
}
