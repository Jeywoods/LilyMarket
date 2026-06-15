namespace LilyMarket.Domain.ValueObjects;

public record UniversityEmail
{
    private const string AllowedDomain = "@sfedu.ru";

    public string Value { get; }

    public UniversityEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var trimmed = email.Trim().ToLowerInvariant();

        if (!trimmed.EndsWith(AllowedDomain))
            throw new ArgumentException(
                $"Email must end with {AllowedDomain}", nameof(email));

        // Проверяем что до @ есть что-то и формат нормальный
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0 || trimmed.Count(c => c == '@') != 1)
            throw new ArgumentException("Invalid email format", nameof(email));

        Value = trimmed;
    }

    public static implicit operator string(UniversityEmail email) => email.Value;
    public override string ToString() => Value;
}