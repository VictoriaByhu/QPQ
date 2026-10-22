using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class SessionFormat
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 50;

    public byte Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";

    private SessionFormat() { }

    public SessionFormat(byte id, string code, string name)
    {
        if (id == 0)
            throw new DomainException("Format id must be positive.");
        if (string.IsNullOrWhiteSpace(code) || code.Length > CodeMaxLength)
            throw new DomainException($"Format code is required and must not exceed {CodeMaxLength} characters.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
            throw new DomainException($"Format name is required and must not exceed {NameMaxLength} characters.");

        Id = id;
        Code = code;
        Name = name;
    }
}
