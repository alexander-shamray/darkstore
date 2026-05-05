using DarkStore.Domain.Users;

namespace DarkStore.UnitTests.Users;

public class UserTests
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_SetsAllProperties()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        var user = User.Create("+77011234567", "Алибек Сейтов", "ali@example.com", "google");

        user.Id.Should().NotBeEmpty();
        user.PhoneNumber.Should().Be("+77011234567");
        user.FullName.Should().Be("Алибек Сейтов");
        user.Email.Should().Be("ali@example.com");
        user.ReferralSource.Should().Be("google");
        user.Corporate.Should().BeFalse();
        user.IsDeleted.Should().BeFalse();
        user.CompanyName.Should().BeNull();
        user.Bin.Should().BeNull();
        user.CreatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_WithoutOptionalFields_SetsNullsCorrectly()
    {
        var user = User.Create("+77011234567", "Тест Юзер");

        user.Email.Should().BeNull();
        user.ReferralSource.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPhoneNumber_ThrowsArgumentException(string phone)
    {
        Func<User> act = () => User.Create(phone, "Тест Юзер");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFullName_ThrowsArgumentException(string fullName)
    {
        Func<User> act = () => User.Create("+77011234567", fullName);

        act.Should().Throw<ArgumentException>();
    }

    // ── MakeCorporate ─────────────────────────────────────────────────────────

    [Fact]
    public void MakeCorporate_SetsCorporateFlagAndCompanyNameAndBin()
    {
        var user = User.Create("+77011234567", "Сейтов Алибек");

        user.MakeCorporate("ТОО ДаркСтор", "123456789012");

        user.Corporate.Should().BeTrue();
        user.CompanyName.Should().Be("ТОО ДаркСтор");
        user.Bin.Should().Be("123456789012");
    }

    [Fact]
    public void MakeCorporate_CanBeCalledTwice_OverwritesValues()
    {
        var user = User.Create("+77011234567", "Сейтов Алибек");
        user.MakeCorporate("ТОО Старый", "111111111111");

        user.MakeCorporate("ТОО Новый", "222222222222");

        user.CompanyName.Should().Be("ТОО Новый");
        user.Bin.Should().Be("222222222222");
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedToTrue()
    {
        var user = User.Create("+77011234567", "Тест");

        user.SoftDelete();

        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_CalledTwice_StaysDeleted()
    {
        var user = User.Create("+77011234567", "Тест");
        user.SoftDelete();

        user.SoftDelete();

        user.IsDeleted.Should().BeTrue();
    }
}


