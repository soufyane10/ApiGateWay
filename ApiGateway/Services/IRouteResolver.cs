namespace ApiGateway.Services
{
    public interface IRouteResolver
    {
        (string? serviceUrl, string? routePrefix) ResolveServiceUrlAndPrefix(string path);
    }
}