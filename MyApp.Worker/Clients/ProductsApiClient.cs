using System.Net.Http.Json;
using MyApp.Worker.Clients.Models;
using OpenTelemetry.Trace;

namespace MyApp.Worker.Clients;

public class ProductsApiClient(HttpClient httpClient, Tracer tracer)
{
    public async Task<Product?> GetProductAsync(string id)
    {
        using var span = tracer.StartActiveSpan("[Products API Client] Get product by id", SpanKind.Client);

        return await httpClient.GetFromJsonAsync<Product>($"/products/{id}");
    }
}