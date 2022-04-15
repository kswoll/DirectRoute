namespace DirectRoute.Endpoints;

/// <summary>
/// Apply this attribute to an endpoint method parameter to indicate that its value should be drawn from the body of the request.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class BodyAttribute : Attribute
{
}