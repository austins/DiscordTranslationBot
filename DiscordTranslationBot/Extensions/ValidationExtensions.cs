using FluentValidation;

namespace DiscordTranslationBot.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> PositiveUInt64<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().Must(x => ulong.TryParse(x, out var num) && num > 0);
    }
}
