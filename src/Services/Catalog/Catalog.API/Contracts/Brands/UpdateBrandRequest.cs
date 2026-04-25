namespace Catalog.API.Contracts.Brands;

public sealed record UpdateBrandRequest(
    string Name,
    string? Description,
    bool IsActive);
