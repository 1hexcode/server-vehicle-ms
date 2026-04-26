using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class ReviewService(AppDbContext db)
{
    public async Task<ApiResponse<ReviewDto>> CreateAsync(Guid customerId, ReviewRequestDto dto)
    {
        if (dto.AppointmentId.HasValue == dto.SalesInvoiceId.HasValue)
            return new ApiResponse<ReviewDto> { Success = false, Message = "Provide exactly one of appointmentId or salesInvoiceId" };

        if (dto.AppointmentId.HasValue)
        {
            var appt = await db.Appointments.FirstOrDefaultAsync(a => a.Id == dto.AppointmentId.Value);
            if (appt == null)
                return new ApiResponse<ReviewDto> { Success = false, Message = "Appointment not found" };
            if (appt.CustomerId != customerId)
                return new ApiResponse<ReviewDto> { Success = false, Message = "Forbidden" };
            if (appt.Status != AppointmentStatus.Completed)
                return new ApiResponse<ReviewDto> { Success = false, Message = "Can only review completed appointments" };
            if (await db.Reviews.AnyAsync(r => r.AppointmentId == appt.Id && r.CustomerId == customerId))
                return new ApiResponse<ReviewDto> { Success = false, Message = "You already reviewed this appointment" };
        }

        var review = new Reviews
        {
            CustomerId = customerId,
            AppointmentId = dto.AppointmentId,
            SalesInvoiceId = dto.SalesInvoiceId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        return new ApiResponse<ReviewDto>
        {
            Success = true,
            Message = "Review submitted",
            Data = ToDto(review, await db.Users.Where(u => u.Id == customerId).Select(u => u.FullName).FirstAsync())
        };
    }

    public async Task<ApiResponse<List<ReviewDto>>> ListAsync(Guid? customerFilter, Guid? appointmentFilter)
    {
        var q = db.Reviews.Include(r => r.Customer).AsQueryable();
        if (customerFilter.HasValue) q = q.Where(r => r.CustomerId == customerFilter.Value);
        if (appointmentFilter.HasValue) q = q.Where(r => r.AppointmentId == appointmentFilter.Value);
        var items = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return new ApiResponse<List<ReviewDto>>
        {
            Success = true,
            Data = items.Select(r => ToDto(r, r.Customer?.FullName ?? "")).ToList()
        };
    }

    public async Task<ApiResponse<ReviewDto>> GetAsync(Guid id)
    {
        var r = await db.Reviews.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return new ApiResponse<ReviewDto> { Success = false, Message = "Review not found" };
        return new ApiResponse<ReviewDto> { Success = true, Data = ToDto(r, r.Customer?.FullName ?? "") };
    }

    private static ReviewDto ToDto(Reviews r, string customerName) => new()
    {
        Id = r.Id,
        CustomerId = r.CustomerId,
        CustomerName = customerName,
        AppointmentId = r.AppointmentId,
        SalesInvoiceId = r.SalesInvoiceId,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt
    };
}
