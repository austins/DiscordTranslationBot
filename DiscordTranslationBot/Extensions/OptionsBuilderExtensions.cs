﻿using FluentValidation;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extension methods for OptionsBuilder.
/// </summary>
public static class OptionsBuilderExtensions
{
    /// <summary>
    /// Enables validation of options using FluentValidation.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <returns>The options builder for chaining.</returns>
    public static OptionsBuilder<TOptions> ValidateWithFluentValidation<TOptions>(
        this OptionsBuilder<TOptions> builder
    ) where TOptions : class
    {
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(
            sp =>
                new FluidValidationOptionsValidator<TOptions>(
                    sp.GetRequiredService<IValidator<TOptions>>(),
                    builder.Name
                )
        );

        return builder;
    }
}

/// <summary>
/// Validator for options.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
public sealed class FluidValidationOptionsValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly string? _name;
    private readonly IValidator<TOptions> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluidValidationOptionsValidator{TOptions}"/> class.
    /// </summary>
    /// <param name="validator">The validator to use.</param>
    /// <param name="name">The name of the option.</param>
    public FluidValidationOptionsValidator(IValidator<TOptions> validator, string? name)
    {
        _validator = validator;
        _name = name;
    }

    /// <summary>
    /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // Null name is used to configure all named options.
        if (_name != null && _name != name)
        {
            // Ignored if not validating this instance.
            return ValidateOptionsResult.Skip;
        }

        // Ensure options are provided to validate against.
        ArgumentNullException.ThrowIfNull(options);

        // Validate the option's values.
        var validationResult = _validator.Validate(options);
        if (validationResult.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        // Format errors on validation failure.
        var errors = validationResult.Errors.Select(
            e =>
                $"Options validation failed for '{options.GetType().Name}.{e.PropertyName}' with error: {e.ErrorMessage}"
        );

        return ValidateOptionsResult.Fail(errors);
    }
}
