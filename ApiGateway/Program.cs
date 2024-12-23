using ApiGateway.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // Register IHttpClientFactory
builder.Services.AddSingleton<IRouteResolver, RouteResolver>();

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .AddSource("ApiGateway") // Optional: Specify a source name for your gateway
        .ConfigureResource(resourceBuilder =>
            resourceBuilder.AddService(serviceName: "ApiGateway", serviceVersion: "1.0.0"))
        .AddAspNetCoreInstrumentation() // Instrument incoming ASP.NET Core requests
        .AddHttpClientInstrumentation() // Instrument outgoing HTTP requests
        .AddConsoleExporter(); // Export traces to the console
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

