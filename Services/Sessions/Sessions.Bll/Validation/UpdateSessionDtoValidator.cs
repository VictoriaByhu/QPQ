using FluentValidation;
using Sessions.Bll.Dtos;

namespace Sessions.Bll.Validation;

public class UpdateSessionDtoValidator : AbstractValidator<UpdateSessionDto>
{
    public UpdateSessionDtoValidator()
    {
        Include(new SessionPlaceValidator());
    }
}
