using FluentAssertions;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Unit.Domain;

public class UniversityEmailTests
{
    [Fact]
    public void ValidEmail_CreatesSuccessfully()
    {
        var email = new UniversityEmail("student@sfedu.ru");
        email.Value.Should().Be("student@sfedu.ru");
    }

    [Fact]
    public void Email_Lowercased()
    {
        var email = new UniversityEmail("Student@SFEDU.RU");
        email.Value.Should().Be("student@sfedu.ru");
    }

    [Fact]
    public void Email_Trimmed()
    {
        var email = new UniversityEmail("  student@sfedu.ru  ");
        email.Value.Should().Be("student@sfedu.ru");
    }

    [Fact]
    public void InvalidDomain_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail("student@gmail.com");
        act.Should().Throw<ArgumentException>().WithMessage("*sfedu.ru*");
    }

    [Fact]
    public void NoAtSign_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail("studentsfedu.ru");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmptyString_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhitespaceString_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MultipleAtSigns_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail("a@b@sfedu.ru");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Null_ThrowsArgumentException()
    {
        var act = () => new UniversityEmail(null!);
        act.Should().Throw<ArgumentException>();
    }
}