using DarkStore.Domain.Couriers;

namespace DarkStore.UnitTests.Couriers;

public class CourierProfileTests
{
    private static readonly Guid _courierId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_SetsAllProperties()
    {
        var profile = CourierProfile.Create(_courierId, "Иванов Иван", "+7 701 123 4567", "900101350012");

        profile.Id.Should().Be(_courierId);
        profile.FullName.Should().Be("Иванов Иван");
        profile.Phone.Should().Be("+7 701 123 4567");
        profile.Iin.Should().Be("900101350012");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFullName_ThrowsArgumentException(string fullName)
    {
        Func<CourierProfile> act = () => CourierProfile.Create(_courierId, fullName, "+7700000000", "900101350012");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPhone_ThrowsArgumentException(string phone)
    {
        Func<CourierProfile> act = () => CourierProfile.Create(_courierId, "Иванов Иван", phone, "900101350012");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankIin_ThrowsArgumentException(string iin)
    {
        Func<CourierProfile> act = () => CourierProfile.Create(_courierId, "Иванов Иван", "+7700000000", iin);

        act.Should().Throw<ArgumentException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ChangesFullNameAndPhone()
    {
        var profile = CourierProfile.Create(_courierId, "Иванов Иван", "+77010001111", "900101350012");

        profile.Update("Петров Пётр", "+77029998888", "850202450023");

        profile.FullName.Should().Be("Петров Пётр");
        profile.Phone.Should().Be("+77029998888");
        profile.Iin.Should().Be("850202450023");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankFullName_ThrowsArgumentException(string fullName)
    {
        var profile = CourierProfile.Create(_courierId, "Иванов Иван", "+77010001111", "900101350012");

        Action act = () => profile.Update(fullName, "+77029998888", "850202450023");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankPhone_ThrowsArgumentException(string phone)
    {
        var profile = CourierProfile.Create(_courierId, "Иванов Иван", "+77010001111", "900101350012");

        Action act = () => profile.Update("Иванов Иван", phone, "900101350012");

        act.Should().Throw<ArgumentException>();
    }
}


