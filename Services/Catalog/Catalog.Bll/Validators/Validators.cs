using Catalog.Bll.Dtos;
using Catalog.Domain.Constants;
using FluentValidation;

namespace Catalog.Bll.Validators;

internal static class CommonRules
{
    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> rule)
        => rule.Matches(CatalogConstants.SlugPattern)
               .WithMessage("Slug may contain only lowercase latin letters, digits and single hyphens.");

    public static IRuleBuilderOptions<T, string?> ValidHttpUrl<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(url => string.IsNullOrWhiteSpace(url)
                            || (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
               .WithMessage("IconUrl must be an absolute http(s) URL.");

    public static IRuleBuilderOptions<T, string?> ValidRowVersion<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(v =>
               {
                   if (v is null) return true;
                   Span<byte> buffer = stackalloc byte[v.Length];
                   return Convert.TryFromBase64String(v, buffer, out _);
               })
               .WithMessage("RowVersion must be a base64 string.");
}

public sealed class CreateSkillDtoValidator : AbstractValidator<CreateSkillDto>
{
    public CreateSkillDtoValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.IconUrl).ValidHttpUrl();
        RuleFor(x => x.TagIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("TagIds must not contain duplicates.");
        RuleForEach(x => x.TagIds).GreaterThan(0);
    }
}

public sealed class UpdateSkillDtoValidator : AbstractValidator<UpdateSkillDto>
{
    public UpdateSkillDtoValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.IconUrl).ValidHttpUrl();
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator() => RuleFor(x => x.Slug).ValidSlug();
}

public sealed class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.RowVersion).ValidRowVersion();
    }
}

public sealed class UpdateSkillLevelDtoValidator : AbstractValidator<UpdateSkillLevelDto>
{
    public UpdateSkillLevelDtoValidator()
        => RuleFor(x => x.Title).Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title must not be blank.");
}
