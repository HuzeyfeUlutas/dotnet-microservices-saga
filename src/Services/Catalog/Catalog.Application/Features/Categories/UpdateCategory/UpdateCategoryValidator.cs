using FluentValidation;

namespace Catalog.Application.Features.Categories.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x)
            .Must(x => x.ParentCategoryId != x.CategoryId)
            .WithMessage("Category cannot be its own parent.");
    }
}
