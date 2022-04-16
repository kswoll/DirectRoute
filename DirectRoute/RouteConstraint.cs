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

    public static RouteConstraint None { get; } = new RouteConstraint("", x => x, RouteConstraintType.None);
    public static RouteConstraint Integer { get; } = Type("int", x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null);
    public static RouteConstraint Boolean { get; } = Type("bool", x => bool.TryParse(x, out var result) ? result : null);
    public static RouteConstraint DateTime { get; } = Type("datetime", x => System.DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null);
    public static RouteConstraint Decimal { get; } = Type("decimal", x => decimal.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null);
    public static RouteConstraint Double { get; } = Type("double", x => double.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null);
    public static RouteConstraint Float { get; } = Type("float", x => float.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null);
    public static RouteConstraint Guid { get; } = Type("guid", x => System.Guid.TryParse(x, out var result) ? result : null);
    public static RouteConstraint Long { get; } = Type("long", x => long.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null);

    public string? Value { get; }

    public string Operator => constraintType switch
    {
        RouteConstraintType.Type => ":",
        RouteConstraintType.Literal => ":",
        _ => ""
    };

    private readonly Func<string, object?> converter;
    private readonly RouteConstraintType constraintType;

    public static RouteConstraint Type(string type, Func<string, object?> converter)
    {
        var constraint = new RouteConstraint(type, converter, RouteConstraintType.Type);
        constraintsByType[type] = constraint;
        return constraint;
    }

    public static RouteConstraint Literal(string value)
    {
        return new RouteConstraint(value, x => x == value ? value : null, RouteConstraintType.Literal);
    }

    private RouteConstraint(string? value, Func<string, object?> converter, RouteConstraintType constraintType)
    {
        Value = value;
        this.converter = converter;
        this.constraintType = constraintType;
    }

    public static RouteConstraint Get(string type) => constraintsByType[type];

    public override string ToString()
    {
        var @operator = constraintType switch
        {
            RouteConstraintType.Type => ":",
            RouteConstraintType.Literal => "=",
            _ => ""
        };

        return $"{@operator}{Value}";
    }

    public object? Convert(string value)
    {
        return converter(value);
    }
}