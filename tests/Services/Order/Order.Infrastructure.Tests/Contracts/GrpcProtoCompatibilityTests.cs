using FluentAssertions;
using Xunit;

namespace Order.Infrastructure.Tests.Contracts;

public class GrpcProtoCompatibilityTests
{
    [Theory]
    [InlineData(
        "src/Services/Catalog/Catalog.API/Grpc/Protos/catalog_purchase_info.proto",
        "src/Services/Order/Order.Infrastructure/Grpc/Protos/catalog_purchase_info.proto")]
    [InlineData(
        "src/Services/Inventory/Inventory.API/Grpc/Protos/inventory_reservation.proto",
        "src/Services/Order/Order.Infrastructure/Grpc/Protos/inventory_reservation.proto")]
    [InlineData(
        "src/Services/Payment/Payment.API/Grpc/Protos/payment_initiation.proto",
        "src/Services/Order/Order.Infrastructure/Grpc/Protos/payment_initiation.proto")]
    public async Task Order_client_proto_should_match_provider_contract(
        string providerRelativePath,
        string orderRelativePath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var providerContract = await ReadNormalizedProtoAsync(repositoryRoot, providerRelativePath);
        var orderContract = await ReadNormalizedProtoAsync(repositoryRoot, orderRelativePath);

        orderContract.Should().Equal(
            providerContract,
            $"'{orderRelativePath}' is the Order-owned client copy of '{providerRelativePath}' and must be updated when the provider contract changes");
    }

    private static async Task<string[]> ReadNormalizedProtoAsync(
        string repositoryRoot,
        string relativePath)
    {
        var absolutePath = Path.Combine(repositoryRoot, relativePath);

        File.Exists(absolutePath).Should().BeTrue($"the proto contract '{relativePath}' must exist");

        return (await File.ReadAllLinesAsync(absolutePath))
            .Select(RemoveLineComment)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static string RemoveLineComment(string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);

        return commentIndex < 0
            ? line
            : line[..commentIndex];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MarketplaceOrderPlatform.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
