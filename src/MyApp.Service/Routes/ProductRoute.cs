using MyApp.Service.Messaging;
using MyApp.Service.Messaging.Protos;
using MyApp.Service.Models;
using OpenTelemetry.Trace;

namespace MyApp.Service.Routes;

public static class ProductsRoute
{
    public static IResult GetProduct(string id, Tracer tracer)
    {
        using var span = tracer.StartActiveSpan("get product by id");

        var product = new Product(id, $"Product {id}", new Random().NextDouble() * 100);

        return TypedResults.Ok(product);
    }

    public static IResult GetAllProducts(Tracer tracer)
    {
        using var span = tracer.StartActiveSpan("get all products");

        var products = new Product[] { };

        return TypedResults.Ok(Task.FromResult(products));
    }

    public static async Task<IResult> CreateProduct(
        Product product,
        ProtobufProducer producer,
        Tracer tracer,
        CancellationToken cancellationToken
    )
    {
        using var span = tracer.StartActiveSpan("create product");

        var productCreated = new ProductCreated
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };

        await producer.ProduceAsync("product-created", productCreated, Guid.NewGuid().ToString(), cancellationToken);

        return TypedResults.Ok();
    }
}