using System.Net;
using Microsoft.Azure.Cosmos;
using MyApp.Service.Models;

namespace MyApp.Service.Data.Cosmos;

public class CosmosRepository
{
    private readonly CosmosClient _cosmosClient;

    public CosmosRepository(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container GetContainer()
    {
        return _cosmosClient.GetDatabase("cosmos")
            .GetContainer("products");
    }

    public Task SaveProduct(Product product, CancellationToken cancellationToken)
    {
        return GetContainer()
            .UpsertItemAsync(product, new PartitionKey(product.Id), cancellationToken: cancellationToken);
    }

    public async Task<Product?> GetProduct(string id, CancellationToken cancellationToken)
    {
        var response = await GetContainer()
            .ReadItemAsync<Product>(id, new PartitionKey(id), cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
            return response.Resource;

        return default;
    }
}