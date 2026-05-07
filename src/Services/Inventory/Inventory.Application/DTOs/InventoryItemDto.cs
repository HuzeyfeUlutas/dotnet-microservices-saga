using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs;

public sealed record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsActive,
    IReadOnlyCollection<InventoryReservationDto> Reservations,
    IReadOnlyCollection<StockMovementDto> StockMovements);

public sealed record InventoryReservationDto(
    Guid Id,
    Guid OrderId,
    int Quantity,
    InventoryReservationStatus Status,
    DateTime ReservedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? ReleasedAtUtc);

public sealed record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    int Quantity,
    string Reason,
    string? ReferenceId,
    DateTime OccurredAtUtc);
