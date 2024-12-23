using System.Diagnostics;

namespace ApiGateway
{
    public static class Telemetry
    {
        public static readonly ActivitySource ActivitySource = new("ApiGateway");
    }
}