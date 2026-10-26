using FluentValidation;

namespace QPQ.Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty().WithMessage("ProductId є обов'язковим.");

        // Звертаємося до властивостей через вкладений об'єкт Author
        RuleFor(v => v.Author.UserId)
            .NotEmpty().WithMessage("UserId є обов'язковим.");

        RuleFor(v => v.Author.Username)
            .NotEmpty().WithMessage("Username є обов'язковим.");

        RuleFor(v => v.Rating)
            .InclusiveBetween(1, 5).WithMessage("Рейтинг має бути від 1 до 5.");

        RuleFor(v => v.Comment)
            .MaximumLength(1000).WithMessage("Коментар не може перевищувати 1000 символів.");
    }
}