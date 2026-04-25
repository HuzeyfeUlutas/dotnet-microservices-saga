using Catalog.Domain.Enums;

namespace Catalog.Application.DTOs;

public sealed record ProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    VariantStatus Status);
