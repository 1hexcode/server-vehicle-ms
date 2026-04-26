using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class StockMovementService(AppDbContext db)
{
    public void Record(Guid partId, int deltaQty, StockMovementReason reason, Guid? refInvoiceId)
    {
        db.StockMovements.Add(new StockMovements
        {
            PartId = partId,
            DeltaQty = deltaQty,
            Reason = reason,
            RefInvoiceId = refInvoiceId
        });
    }

    public async Task<ApiResponse<List<StockMovementDto>>> ListAsync(Guid? partId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var q = db.StockMovements.Include(m => m.Part).AsQueryable();
        if (partId.HasValue) q = q.Where(m => m.PartId == partId.Value);
        if (from.HasValue) q = q.Where(m => m.OccurredAt >= from.Value);
        if (to.HasValue) q = q.Where(m => m.OccurredAt <= to.Value);

        var items = await q.OrderByDescending(m => m.OccurredAt).ToListAsync();
        return new ApiResponse<List<StockMovementDto>>
        {
            Success = true,
            Data = items.Select(ToDto).ToList()
        };
    }

    private static StockMovementDto ToDto(StockMovements m) => new()
    {
        Id = m.Id,
        PartId = m.PartId,
        PartName = m.Part?.Name,
        Sku = m.Part?.Sku,
        DeltaQty = m.DeltaQty,
        Reason = m.Reason.ToString(),
        RefInvoiceId = m.RefInvoiceId,
        OccurredAt = m.OccurredAt
    };
}
