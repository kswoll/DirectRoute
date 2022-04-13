using System.Globalization;

namespace DirectRoute;

/// <summary>
/// Represents the route constraints in paths.  For example in "/path/to/{Value:int}", int is the constraint.
/// </summary>
public class RouteConstraint
{
    /// <summary>
    /// This needs to be at the top of the class since creating an instance of RouteConstraint tries to add to this dictionary.
    /// </summary>
    private static readonly Dictionary<string, RouteConstraint> constraintsByType = new();

    public static RouteConstraint None { get; } = new RouteConstraint(null, x => x);
    public static RouteConstraint Integer { get; } = new RouteConstraint("int", x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0);
    public static RouteConstraint Boolean { get; } = new RouteConstraint("bool", x => bool.TryParse(x, out var result) ? result : 0);
    public static RouteConstraint DateTime { get; } = new RouteConstraint("datetime", x => System.DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : 0);
    public static RouteConstraint Decimal { get; } = new RouteConstraint("decimal", x => decimal.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0);
    public static RouteConstraint Double { get; } = new RouteConstraint("double", x => double.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0);
    public static RouteConstraint Float { get; } = new RouteConstraint("float", x => float.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0);
    public static RouteConstraint Guid { get; } = new RouteConstraint("guid", x => System.Guid.TryParse(x, out var result) ? result : 0);
    public static RouteConstraint Long { get; } = new RouteConstraint("long", x => long.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0);

    public string? Type { get; }

    private readonly Func<string, object> converter;

    public RouteConstraint(string? type, Func<string, object> converter)
    {
        Type = type;
        this.converter = converter;

        if (Type != null)
            constraintsByType[Type] = this;
    }

    public static RouteConstraint Get(string type) => constraintsByType[type];

    public object Convert(string value)
    {
        return converter(value);
    }
}