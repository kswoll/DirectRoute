namespace DirectRoute;

public class RoutePart
{
    public RoutePartType Type { get; set; }
    public string? Text { get; set; }
    public string? Variable { get; set; }
    public RouteConstraint? Constraint { get; set; }

    public RoutePart(RoutePartType type, string value, RouteConstraint? constraint = null)
    {
        Type = type;
        if (type == RoutePartType.Text)
        {
            Text = value;
        }
        else
        {
            Variable = value;
            Constraint = constraint;
        }
    }

    public override string ToString()
    {
        return Type switch
        {
            RoutePartType.Text => Text!,
            _ => $"{{{(Constraint == null ? Variable : $"{Variable}:{Constraint.Type}")}}}",
        };
    }

    public object Convert(string value)
    {
        if (Constraint != null)
            return Constraint.Convert(value);
        else
            return value;
    }
}