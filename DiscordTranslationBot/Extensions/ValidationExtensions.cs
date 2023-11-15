using FluentValidation;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extensions for Fluent Validation.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates that a string is a positive ulong.
    /// </summary>
    /// <param name="ruleBuilder">Fluent Validation rule builder.</param>
    /// <typeparam name="T">Type of object being validated</typeparam>
    /// <returns></returns>
    public static IRuleBuilderOptions<T, string> PositiveUInt64<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().Must(x => ulong.TryParse(x, out var num) && num > 0);
    }
}
