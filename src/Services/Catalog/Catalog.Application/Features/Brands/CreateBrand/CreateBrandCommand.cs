using MediatR;

namespace Catalog.Application.Features.Brands.CreateBrand;

public sealed record CreateBrandCommand(
    string Name,
    string? Description) : IRequest<Guid>;
