using System.Collections.Immutable;
using System.Text;

namespace DirectRoute;

/// <summary>
/// Represents a route path (often called a "template" in framework code) of the form "entities/{EntityId}/resource".
/// These are the values you are familiar with in both Blazor and Web Api to define the routes for your pages and
/// API endpoints.  This class is responsible for:
///
/// * Being able to parse those paths into segments (separated by the slash / character) where each segment is either
///   a string literal or a variable substitution.  These are represented as an instance of RoutePart.
/// * Being able to obtain the original template (by reconstructing it)
/// * Formatting into a relative url incorporating both route value substitution and query string handling.
/// * Matching an existing url and extracting the provided route values into a dictionary of key value pairs (the route dictionary).
/// </summary>
public class RoutePath
{
    public IReadOnlyList<RoutePart> Parts => parts;
    public IEnumerable<RoutePart> Variables => parts.Where(x => x.Type == RoutePartType.Variable);

    private readonly List<RoutePart> parts = new();

    private RoutePath(IEnumerable<RoutePart> parts)
    {
        this.parts = parts.ToList();
    }

    /// <summary>
    /// Reconstructs the original path that was sent to the parse method.
    /// </summary>
    public string Value => string.Join('/', parts.Select(x => x));

    /// <summary>
    /// Primarily responsible for converting the provided routeValues array into a route dictionary with the keys
    /// derived from the route parts representing variables.  Also returns an indication of whether this RoutePath
    /// actually matches the provided path.
    /// </summary>
    /// <param name="path">The URL path you are matching against</param>
    /// <param name="routeValues">The route values that should be substituted in to form the route dictionary</param>
    /// <returns>A RouteMatch object indicating whether it's a successful match and the route dictionary</returns>
    public RouteMatch Match(string path, params object[] routeValues)
    {
        return Match(path.Split('/'), routeValues);
    }

    /// <summary>
    /// Primarily responsible for converting the provided routeValues array into a route dictionary with the keys
    /// derived from the route parts representing variables.  Also returns an indication of whether this RoutePath
    /// actually matches the provided pathSegments.
    /// </summary>
    /// <param name="pathSegments">The URL path you are matching against after being split across '/' into separate segments</param>
    /// <param name="routeValues">The route values that should be substituted in to form the route dictionary</param>
    /// <returns>A RouteMatch object indicating whether it's a successful match and the route dictionary</returns>
    public RouteMatch Match(string[] pathSegments, object[] routeValues)
    {
        if (pathSegments.Length != parts.Count)
            return false;

        var routeArguments = ImmutableDictionary<string, object>.Empty;
        var routeValuesQueue = new Queue<object>(routeValues);

        foreach (var (i, part) in parts.Enumerate())
        {
            var pathSegment = pathSegments[i];

            switch (part.Type)
            {
                case RoutePartType.Text:
                    if (pathSegment != part.Text)
                        return false;
                    break;
                case RoutePartType.Variable:
                    var routeParameter = routeValuesQueue.Dequeue();
                    routeArguments = routeArguments.Add(part.Variable!, routeParameter);
                    break;
            }
        }

        return new RouteMatch(new Dictionary<string, object>(routeArguments));
    }

    /// <summary>
    /// Parses the provided string into a RoutePath.  These strings should folow the convention of route templates
    /// seen in Blazor and Web Api.
    /// </summary>
    /// <param name="s">The path to parse</param>
    /// <returns>A new instance of RoutePath representing the provided path</returns>
    public static RoutePath Parse(string s)
    {
        var segments = s.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var parts = new List<RoutePart>();
        foreach (var segment in segments)
        {
            // If variable
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var variable = segment[1..^1];
                var variableParts = variable.SplitAndIncludeDelimiters(new[] { ':', '=' });
                var constraintTypeString = variableParts.ElementAtOrDefault(1);
                var constraintType = constraintTypeString switch
                {
                    "=" => RouteConstraintType.Literal,
                    ":" => RouteConstraintType.Type,
                    _ => RouteConstraintType.None
                };
                var variableType = variableParts.ElementAtOrDefault(2);
                if (constraintType != RouteConstraintType.None && variableType == null)
                    throw new InvalidOperationException($"Invalid syntax for route constraint.  Missing right hand side of '{constraintTypeString}' in route '{s}'");

                var constraint = constraintType switch
                {
                    RouteConstraintType.Type => RouteConstraint.Get(variableType!),
                    RouteConstraintType.Literal => RouteConstraint.Literal(variableType!),
                    _ => null
                };

                var variablePart = new RoutePart(RoutePartType.Variable, variableParts[0], constraint);
                parts.Add(variablePart);
            }
            else
            {
                var textPart = new RoutePart(RoutePartType.Text, segment);
                parts.Add(textPart);
            }
        }

        return new RoutePath(parts);
    }

    /// <summary>
    /// Formats this RoutePath instance into a relative url incorporating the provided routeArguments and
    /// queryString.  Note that these objects can either be an instance of Dictionary{string, object} or
    /// else uses reflection to convert the properties in the object into a dictionary.
    /// </summary>
    public string FormatUrl(object? routeArguments, object? queryString)
    {
        var url = FormatPath(routeArguments).Query(queryString);
        return url;
    }

    /// <summary>
    /// Formats this RoutePath instance into a relative url incorporating the provided routeArguments and
    /// queryString.  Note that queryString can either be an instance of Dictionary{string, object} or
    /// else uses reflection to convert the properties in the object into a dictionary.  Note: the number
    /// of elements in routeArguments should match the number of variables in this path.
    /// </summary>
    public string FormatUrl(object[] routeArguments, object? queryString)
    {
        var url = FormatPath(routeArguments).Query(queryString);
        return url;
    }

    private string FormatPath(object[] routeArguments)
    {
        if (routeArguments is null)
            throw new ArgumentNullException(nameof(routeArguments));
        if (routeArguments.Length != Variables.Where(x => x.Constraint.IsPathValueRequired).Count())
            throw new ArgumentException($"{nameof(routeArguments.Length)} must contain the same number of elements as this path supports ({Value}), received {routeArguments} arguments but {Variables.Count()} variables: {string.Join(", ", Variables.Select(x => x.Variable))}");

        var result = new StringBuilder();
        var routeArgumentQueue = new Queue<object>(routeArguments);
        foreach (var (i, part) in parts.Enumerate())
        {
            if (part.Type == RoutePartType.Text)
            {
                result.Append(part.Text);
            }
            else
            {
                object? value;
                if (part.Constraint.IsPathValueRequired)
                    value = routeArgumentQueue.Dequeue();
                else
                    value = part.Constraint.Value;

                try
                {
                    result.Append(Convert.ChangeType(value, typeof(string)));
                }
                catch (InvalidCastException ex)
                {
                    throw new InvalidOperationException($"Unable to convert the value '{value}' of type '{value?.GetType().FullName ?? "null"}' to type 'string' for route path '{Value}'", ex);
                }
            }

            if (i < parts.Count - 1)
                result.Append('/');
        }
        return result.ToString();
    }

    private string FormatPath(object? routeArguments)
    {
        var result = new StringBuilder();
        var routeDictionary = routeArguments?.ToDictionary();
        foreach (var (i, part) in parts.Enumerate())
        {
            if (part.Type == RoutePartType.Text)
            {
                result.Append(part.Text);
            }
            else
            {
                var variableParts = part.Variable!.Split(':');
                object? value;
                if (part.Constraint.IsPathValueRequired)
                {
                    if (routeDictionary == null || !routeDictionary.TryGetValue(variableParts[0], out value))
                    {
                        throw new Exception($"Cannot format {Value}.  Provided {nameof(routeArguments)} missing value for {part.Variable}");
                    }
                }
                else
                {
                    value = part.Constraint.Value;
                }

                result.Append(value?.ToString());
            }

            if (i < parts.Count - 1)
                result.Append('/');
        }
        return result.ToString();
    }
}