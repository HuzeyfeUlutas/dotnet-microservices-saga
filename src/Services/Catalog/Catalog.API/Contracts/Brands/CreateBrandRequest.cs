namespace Catalog.API.Contracts.Brands;

public sealed record CreateBrandRequest(
    string Name,
    string? Description);
