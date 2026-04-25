using MediatR;

namespace Catalog.Application.Features.Brands.UpdateBrand;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string Name,
    string? Description,
    bool IsActive) : IRequest;
