using FluentValidation;
using QPQ.Moderation.Bll.DTOs;

namespace QPQ.Moderation.Bll.Validators;

public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок скарги обов'язковий.")
            .MaximumLength(100).WithMessage("Заголовок не може перевищувати 100 символів.");

        // Якщо в тебе є поле Description, розкоментуй і його:
        // RuleFor(x => x.Description)
        //     .NotEmpty().WithMessage("Опис скарги обов'язковий.")
        //     .MaximumLength(500).WithMessage("Опис не може перевищувати 500 символів.");
    }
}