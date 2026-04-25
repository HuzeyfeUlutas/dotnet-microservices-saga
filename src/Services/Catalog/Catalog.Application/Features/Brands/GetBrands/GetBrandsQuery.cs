using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Brands.GetBrands;

public sealed record GetBrandsQuery : IRequest<IReadOnlyCollection<BrandListItemDto>>;
