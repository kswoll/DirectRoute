namespace DirectRoute.Tests;

[TestFixture]
public class RouteConstraintTests
{
    [Test]
    public void None()
    {
        var constraint = RouteConstraint.None;
        var s = new string("test");
        var result = constraint.Convert(s);
        ReferenceEquals(s, result).ShouldBeTrue();
    }

    [Test]
    public void Int()
    {
        var constraint = RouteConstraint.Get("int");
        var expected = 42;
        var result = (int)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Long()
    {
        var constraint = RouteConstraint.Get("long");
        var expected = 42L;
        var result = (long)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Float()
    {
        var constraint = RouteConstraint.Get("float");
        var expected = 1.23F;
        var result = (float)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Double()
    {
        var constraint = RouteConstraint.Get("double");
        var expected = 1.23D;
        var result = (double)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Decimal()
    {
        var constraint = RouteConstraint.Get("decimal");
        var expected = 1.23M;
        var result = (decimal)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Boolean()
    {
        var constraint = RouteConstraint.Get("bool");
        var expected = true;
        var result = (bool)constraint.Convert(expected.ToString(CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void Date()
    {
        var constraint = RouteConstraint.Get("datetime");
        var expected = new DateTime(2022, 1, 2);
        var result = (DateTime)constraint.Convert(expected.ToString("yyyy-M-d", CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }

    [Test]
    public void DateTime()
    {
        var constraint = RouteConstraint.Get("datetime");
        var expected = new DateTime(2022, 1, 2, 4, 5, 6);
        var result = (DateTime)constraint.Convert(expected.ToString("yyyy-M-d hh\\:mm\\:ss", CultureInfo.InvariantCulture))!;
        result.ShouldBe(expected);
    }
}