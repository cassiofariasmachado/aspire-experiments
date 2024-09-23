using MyApp.Service.Models;
using OpenTelemetry.Trace;

namespace MyApp.Service.Clients;

public class ProductsApiClient(HttpClient httpClient, Tracer tracer)
{
    public async Task<Product?> GetProductAsync(string id)
    {
        using var span = tracer.StartActiveSpan("get product by id", SpanKind.Client);

        return await httpClient.GetFromJsonAsync<Product>($"/products/{id}");
    }
}