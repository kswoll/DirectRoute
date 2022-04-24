namespace DirectRoute.Endpoints
{
    public static class RouteExtensions
    {
        public static Route? Get(this IRoutes[] routesArray, Type endpointType)
        {
            foreach (var routes in routesArray)
            {
                var result = routes.Get(endpointType);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
