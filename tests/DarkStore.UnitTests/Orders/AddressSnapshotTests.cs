using DarkStore.Domain.Orders;

namespace DarkStore.UnitTests.Orders;

public class AddressSnapshotTests
{
    [Fact]
    public void Record_Equality_WorksByValue()
    {
        var a = new AddressSnapshot("ул. Ленина", "5", "3", 47.1m, 51.9m, "ул. Ленина, 5, кв. 3");
        var b = new AddressSnapshot("ул. Ленина", "5", "3", 47.1m, 51.9m, "ул. Ленина, 5, кв. 3");

        a.Should().Be(b);
    }

    [Fact]
    public void Record_WithSameValues_HaveSameHashCode()
    {
        var a = new AddressSnapshot("ул. Ленина", "5", null, 47.1m, 51.9m, "ул. Ленина, 5");
        var b = new AddressSnapshot("ул. Ленина", "5", null, 47.1m, 51.9m, "ул. Ленина, 5");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Record_WithDifferentValues_AreNotEqual()
    {
        var a = new AddressSnapshot("ул. Ленина", "5", null, 47.1m, 51.9m, "ул. Ленина, 5");
        var b = new AddressSnapshot("ул. Ленина", "6", null, 47.1m, 51.9m, "ул. Ленина, 6");

        a.Should().NotBe(b);
    }
}


