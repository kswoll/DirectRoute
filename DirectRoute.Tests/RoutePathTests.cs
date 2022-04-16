namespace DirectRoute.Tests;

[TestFixture]
public class RoutePathTests
{
    [Test]
    public void ParseOnlyText()
    {
        var routePath = RoutePath.Parse("api/endpoint");
        routePath.Parts.Count.ShouldBe(2);
        routePath.Parts[0].Type.ShouldBe(RoutePartType.Text);
        routePath.Parts[0].Text.ShouldBe("api");
        routePath.Parts[1].Type.ShouldBe(RoutePartType.Text);
        routePath.Parts[1].Text.ShouldBe("endpoint");
        routePath.Parts[0].Variable.ShouldBeNull();
        routePath.Parts[0].Constraint.ShouldBeNull();
        routePath.Parts[1].Variable.ShouldBeNull();
        routePath.Parts[1].Constraint.ShouldBeNull();
    }

    [Test]
    public void ParseOnlyVariableWithoutType()
    {
        var routePath = RoutePath.Parse("{id}");
        routePath.Parts.Count.ShouldBe(1);
        routePath.Parts[0].Type.ShouldBe(RoutePartType.Variable);
        routePath.Parts[0].Text.ShouldBeNull();
        routePath.Parts[0].Variable.ShouldBe("id");
        routePath.Parts[0].Constraint.ShouldBeNull();
    }

    [Test]
    public void ParseOnlyVariableWithType()
    {
        var routePath = RoutePath.Parse("{id:int}");
        routePath.Parts.Count.ShouldBe(1);
        routePath.Parts[0].Type.ShouldBe(RoutePartType.Variable);
        routePath.Parts[0].Text.ShouldBeNull();
        routePath.Parts[0].Variable.ShouldBe("id");
        routePath.Parts[0].Constraint!.Value.ShouldBe("int");
    }

    [Test]
    public void ParseOnlyVariableWithLiteral()
    {
        var routePath = RoutePath.Parse("{id=5}");
        routePath.Parts.Count.ShouldBe(1);
        routePath.Parts[0].Type.ShouldBe(RoutePartType.Variable);
        routePath.Parts[0].Text.ShouldBeNull();
        routePath.Parts[0].Variable.ShouldBe("id");
        routePath.Parts[0].Constraint!.ConstraintType.ShouldBe(RouteConstraintType.Literal);
        routePath.Parts[0].Constraint!.Value.ShouldBe("5");
    }

    [Test]
    public void ParseTextThenVariable()
    {
        var routePath = RoutePath.Parse("api/endpoint/{id}");
        routePath.Parts.Count.ShouldBe(3);
        routePath.Parts[0].Text.ShouldBe("api");
        routePath.Parts[1].Text.ShouldBe("endpoint");
        routePath.Parts[2].Variable.ShouldBe("id");
    }

    [Test]
    public void ParseTextThenVariableThenText()
    {
        var routePath = RoutePath.Parse("api/endpoint/{id}/service");
        routePath.Parts.Count.ShouldBe(4);
        routePath.Parts[0].Text.ShouldBe("api");
        routePath.Parts[1].Text.ShouldBe("endpoint");
        routePath.Parts[2].Variable.ShouldBe("id");
        routePath.Parts[3].Text.ShouldBe("service");
    }

    [Test]
    public void MatchText()
    {
        var routePath = RoutePath.Parse("path/to/page");
        routePath.Match("path/to/page", Array.Empty<object>()).IsMatch.ShouldBeTrue();
    }

    [Test]
    public void MatchVariable()
    {
        var routePath = RoutePath.Parse("{variable}");
        routePath.Match("42", 42).IsMatch.ShouldBeTrue();
    }

    [Test]
    public void MatchTextFails()
    {
        var routePath = RoutePath.Parse("path/to/page");
        routePath.Match("path/not/page").IsMatch.ShouldBeFalse();
    }

    [Test]
    public void MatchVariableFails()
    {
        var routePath = RoutePath.Parse("leagues/{LeagueCode}");
        routePath.Match("leagues/XOT/rotations").IsMatch.ShouldBeFalse();
    }

    [Test]
    public void MatchCastsToType()
    {
        var routePath = RoutePath.Parse("leagues/{Id:int}/schedule");
        var match = routePath.Match("leagues/5/schedule", 5);
        match.IsMatch.ShouldBeTrue();

        var id = match.RouteArguments["Id"];
        (id is int).ShouldBeTrue();
    }

    [Test]
    public void MatchLiteral()
    {
        var routePath = RoutePath.Parse("leagues/{Id=5}/schedule");
        var match = routePath.Match("leagues/5/schedule", 5);
        match.IsMatch.ShouldBeTrue();

        var id = match.RouteArguments["Id"];
        (id is int).ShouldBeTrue();
    }
}