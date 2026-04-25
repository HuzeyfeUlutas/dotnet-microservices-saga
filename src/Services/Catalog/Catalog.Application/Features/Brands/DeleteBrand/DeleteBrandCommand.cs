using MediatR;

namespace Catalog.Application.Features.Brands.DeleteBrand;

public sealed record DeleteBrandCommand(Guid BrandId) : IRequest;
