using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Brands.GetBrandById;

public sealed record GetBrandByIdQuery(Guid BrandId) : IRequest<BrandDto>;
