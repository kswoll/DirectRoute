namespace DirectRoute.Blazor;

public abstract class PageRoutes
{
    public IReadOnlyList<PageRoute> Routes => routes;

    private readonly List<PageRoute> routes = new();

    /// <summary>
    /// Must be called by your implementation in the constructor.  We can't make this method abstract
    /// and call it in this class' constructor because the subclass' properties won't be initialized
    /// yet.
    /// </summary>
    protected void InitializeRoutes()
    {
        foreach (var property in GetType().GetProperties().Where(x => typeof(PageRoute).IsAssignableFrom(x.PropertyType)))
        {
            var value = (PageRoute?)property.GetValue(this);
            if (value == null)
                throw new Exception($"Property {property.Name} in {GetType().FullName} is of type PageRoute, but the property was null.  PageRoute properties must be initialized before calling InitializeRoutes()");
            routes.Add(value);
        }
    }
}