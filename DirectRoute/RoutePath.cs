using System.Collections.Immutable;
using System.Text;

namespace DirectRoute;

public class RoutePath
{
    public IReadOnlyList<RoutePart> Parts => parts;
    public IEnumerable<RoutePart> Variables => parts.Where(x => x.Type == RoutePartType.Variable);

    private readonly List<RoutePart> parts = new();

    public RoutePath(IEnumerable<RoutePart> parts)
    {
        this.parts = parts.ToList();
    }

    public string Value => string.Join('/', parts.Select(x => x));

    public RouteMatch Match(string path, params object[] routeValues)
    {
        return Match(path.Split('/'), routeValues);
    }

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
                var variableParts = variable.Split(':');
                var variableType = variableParts.ElementAtOrDefault(1);
                var constraint = variableType == null ? null : RouteConstraint.Get(variableType);
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

    public string FormatUrl(object? routeArguments, object? queryString)
    {
        var url = FormatPath(routeArguments).Query(queryString);
        return url;
    }

    public string FormatUrl(object[] routeArguments, object? queryString)
    {
        var url = FormatPath(routeArguments).Query(queryString);
        return url;
    }

    private string FormatPath(object[] routeArguments)
    {
        if (routeArguments is null)
            throw new ArgumentNullException(nameof(routeArguments));
        if (routeArguments.Length != Variables.Count())
            throw new ArgumentException($"{nameof(routeArguments)} must contain the same number of elements as this path supports");

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
                var value = routeArgumentQueue.Dequeue();
                result.Append(Convert.ChangeType(value, typeof(string)));
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
                if (routeDictionary == null || !routeDictionary.TryGetValue(variableParts[0], out var value))
                {
                    throw new Exception($"Cannot format {Value}.  Provided {nameof(routeArguments)} missing value for {part.Variable}");
                }
                result.Append(value?.ToString());
            }

            if (i < parts.Count - 1)
                result.Append('/');
        }
        return result.ToString();
    }
}