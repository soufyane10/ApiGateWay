using Microsoft.Extensions.Configuration;

namespace ApiGateway.Services
{
    public class RouteResolver : IRouteResolver
    {
        private readonly IConfiguration _configuration;

        public RouteResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string? serviceUrl, string? routePrefix) ResolveServiceUrlAndPrefix(string path)
        {
            var routes = _configuration.GetSection("Routes").GetChildren();
            foreach (var route in routes)
            {
                if (path.StartsWith(route.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return (route.Value, route.Key);
                }
            }
            return (null, null);
        }
    }
}