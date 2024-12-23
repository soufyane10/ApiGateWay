using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ApiGateway.Services;
using Polly;
using Polly.CircuitBreaker;
using Internal;
using System;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("{*catchAll}")]
    public class GatewayController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRouteResolver _routeResolver;
        private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;

        public GatewayController(IHttpClientFactory httpClientFactory, IRouteResolver routeResolver)
        {
            _httpClientFactory = httpClientFactory;
            _routeResolver = routeResolver;

            // Define the Circuit Breaker policy
            _circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>() // Handle exceptions during the request
                .OrTransientHttpStatusCode(System.Net.HttpStatusCode.InternalServerError) // Or specific HTTP status codes indicating transient failures
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2, // Number of consecutive failures before opening the circuit
                    durationOfBreak: TimeSpan.FromSeconds(30), // Duration the circuit stays open
                    onBreak: (exception, timespan) =>
                    {
                        // Log or track when the circuit breaks
                        Console.WriteLine($"Circuit Breaker opened for {timespan.TotalSeconds} seconds due to: {exception.Message}");
                    },
                    onReset: () =>
                    {
                        // Log or track when the circuit resets
                        Console.WriteLine("Circuit Breaker reset.");
                    },
                    onHalfOpen: () =>
                    {
                        // Log or track when the circuit enters the half-open state
                        Console.WriteLine("Circuit Breaker in half-open state. Probing backend.");
                    }
                );
        }

        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        [HttpPatch]
        public async Task<IActionResult> HandleRequest()
        {
            var request = HttpContext.Request;
            string? requestPathValue = request.Path.Value;

            if (requestPathValue != null)
            {
                var (targetServiceUrl, matchedRoutePrefix) = _routeResolver.ResolveServiceUrlAndPrefix(requestPathValue);

                if (string.IsNullOrEmpty(targetServiceUrl))
                {
                    return NotFound();
                }

                var targetPath = requestPathValue.Substring(matchedRoutePrefix?.Length ?? 0);
                var targetUri = new Uri(new Uri(targetServiceUrl), targetPath);

                var httpClient = _httpClientFactory.CreateClient();
                var httpRequestMessage = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

                foreach (var header in request.Headers)
                {
                    if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                if (request.ContentLength > 0 && (request.Method == HttpMethod.Post.Method || request.Method == HttpMethod.Put.Method || request.Method == HttpMethod.Patch.Method))
                {
                    var content = new StreamContent(request.Body);
                    if (!string.IsNullOrEmpty(request.ContentType))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
                    }
                    httpRequestMessage.Content = content;
                }

                HttpResponseMessage response;
                try
                {
                    // Execute the request through the Circuit Breaker policy
                    response = await _circuitBreakerPolicy.ExecuteAsync(() => httpClient.SendAsync(httpRequestMessage));
                }
                catch (BrokenCircuitException ex)
                {
                    // Handle the case where the circuit is open
                    Console.WriteLine($"Circuit is open. Request blocked: {ex.Message}");
                    return StatusCode(503, "Backend service is temporarily unavailable."); // Return Service Unavailable
                }

                foreach (var header in response.Headers)
                {
                    Response.Headers[header.Key] = header.Value.ToArray();
                }

                var responseContent = await response.Content.ReadAsStreamAsync();
                string? contentType = response.Content.Headers.ContentType?.MediaType;
                return StatusCode((int)response.StatusCode, new FileStreamResult(responseContent, contentType ?? "application/octet-stream"));
            }
            else
            {
                return BadRequest("Request path is missing.");
            }
        }
    }
}