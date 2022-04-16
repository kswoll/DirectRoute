namespace DirectRoute.Blazor;

/// <summary>
/// A class designed to optimize resolving paths by caching at each segment level.
/// </summary>
internal class PageRouteTree
{
    private readonly Node root = new();

    public PageRouteTree(IEnumerable<PageRoute> routes)
    {
        foreach (var route in routes)
        {
            var node = root;

            void SetRoute()
            {
                if (node.Route != null)
                    throw new Exception($"Ambiguous routes found:\r\n{route.Path} at {route.PageType.FullName}\r\n{node.Route.Path} at {node.Route.PageType.FullName}");

                node.Route = route;
            }

            if (route.Parts.Count == 0)
            {
                SetRoute();
            }
            else
            {
                foreach (var (i, part) in route.Parts.Enumerate())
                {
                    node = node.Add(part);

                    if (i == route.Parts.Count - 1)
                        SetRoute();
                }
            }
        }
    }

    public (PageRoute? Page, object[] RouteValues) Resolve(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var routeValues = new List<object>();
        Node? node = root;
        foreach (var segment in segments)
        {
            (node, var routeValue, var hasRouteValue) = node!.Resolve(segment);
            if (node == null)
                break;
            if (hasRouteValue)
                routeValues.Add(routeValue!);
        }

        return (node?.Route, routeValues.ToArray());
    }

    public class Node
    {
        public PageRoute? Route { get; set; }

        private readonly Dictionary<string, Node> children = new();
        private readonly Dictionary<RouteConstraint, Node> wildcards = new();

        public Node Add(RoutePart part)
        {
            if (part.Type == RoutePartType.Variable)
            {
                if (part.Constraint != null)
                {
                    if (!wildcards.TryGetValue(part.Constraint, out var wildcard))
                        wildcards[part.Constraint] = wildcard = new Node();
                    return wildcard;
                }
                else
                {
                    if (!wildcards.TryGetValue(RouteConstraint.None, out var wildcard))
                        wildcards[RouteConstraint.None] = wildcard = new Node();
                    return wildcard;
                }
            }
            else
            {
                if (!children.TryGetValue(part.Text!, out var node))
                {
                    node = new Node();
                    children[part.Text!] = node;
                }
                return node;
            }
        }

        public (Node? Node, object? RouteValue, bool HasRouteValue) Resolve(string segment)
        {
            if (children.TryGetValue(segment, out var node))
            {
                return (node, null, false);
            }
            else
            {
                Node? noConstraintWildcard = null;

                foreach (var (constraint, wildcard) in wildcards)
                {
                    if (constraint != RouteConstraint.None)
                    {
                        var convertedValue = constraint.Convert(segment);
                        if (convertedValue != null)
                            return (wildcard, convertedValue, true);
                    }
                    else
                    {
                        noConstraintWildcard = wildcard;
                    }
                }

                if (noConstraintWildcard != null)
                    return (noConstraintWildcard, segment, true);
            }

            return (null, null, false);
        }
    }
}