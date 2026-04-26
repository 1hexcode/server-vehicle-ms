using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class AppointmentService(AppDbContext db, NotificationService notifications)
{
    public async Task<ApiResponse<AppointmentDto>> CreateAsync(Guid customerId, AppointmentRequestDto dto)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId);
        if (vehicle == null)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Vehicle not found" };
        if (vehicle.CustomerId != customerId)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Vehicle does not belong to you" };

        var appt = new Appointments
        {
            CustomerId = customerId,
            VehicleId = vehicle.Id,
            ServiceType = dto.ServiceType,
            RequestedAt = dto.RequestedAt,
            Status = AppointmentStatus.Pending,
            Notes = dto.Notes,
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();
        return new ApiResponse<AppointmentDto> { Success = true, Message = "Appointment booked", Data = await LoadDtoAsync(appt.Id) };
    }

    public async Task<ApiResponse<List<AppointmentDto>>> ListAsync(Guid? customerFilter)
    {
        var q = db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .Include(a => a.AssignedStaff)
            .AsQueryable();
        if (customerFilter.HasValue) q = q.Where(a => a.CustomerId == customerFilter.Value);

        var items = await q.OrderByDescending(a => a.RequestedAt).ToListAsync();
        return new ApiResponse<List<AppointmentDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<AppointmentDto>> GetAsync(Guid id, Guid? customerFilter)
    {
        var appt = await db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .Include(a => a.AssignedStaff)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt == null)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Appointment not found" };
        if (customerFilter.HasValue && appt.CustomerId != customerFilter.Value)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Forbidden" };
        return new ApiResponse<AppointmentDto> { Success = true, Data = ToDto(appt) };
    }

    public async Task<ApiResponse<AppointmentDto>> UpdateStatusAsync(Guid id, AppointmentStatusUpdateDto dto)
    {
        var appt = await db.Appointments
            .Include(a => a.Customer).Include(a => a.Vehicle).Include(a => a.AssignedStaff)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt == null)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Appointment not found" };

        if (dto.AssignedStaffUserId.HasValue)
        {
            var staff = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.AssignedStaffUserId.Value);
            if (staff == null || staff.Role != UserRoles.Staff)
                return new ApiResponse<AppointmentDto> { Success = false, Message = "Assigned user must be Staff" };
            appt.AssignedStaffUserId = staff.Id;
            appt.AssignedStaff = staff;
        }

        appt.Status = dto.Status;
        if (dto.Status == AppointmentStatus.Confirmed && appt.ConfirmedAt == null)
            appt.ConfirmedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            appt.Notes = dto.Notes;

        await notifications.EnqueueAsync(
            appt.CustomerId,
            $"Appointment {dto.Status}",
            $"Your appointment on {appt.RequestedAt:yyyy-MM-dd HH:mm} is now {dto.Status}.",
            NotificationType.AppointmentUpdate);

        await db.SaveChangesAsync();
        return new ApiResponse<AppointmentDto> { Success = true, Message = "Appointment updated", Data = ToDto(appt) };
    }

    public async Task<ApiResponse<AppointmentDto>> CancelOwnAsync(Guid id, Guid customerId)
    {
        var appt = await db.Appointments
            .Include(a => a.Customer).Include(a => a.Vehicle).Include(a => a.AssignedStaff)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt == null)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Appointment not found" };
        if (appt.CustomerId != customerId)
            return new ApiResponse<AppointmentDto> { Success = false, Message = "Forbidden" };
        if (appt.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return new ApiResponse<AppointmentDto> { Success = false, Message = $"Cannot cancel a {appt.Status} appointment" };

        appt.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();
        return new ApiResponse<AppointmentDto> { Success = true, Message = "Appointment cancelled", Data = ToDto(appt) };
    }

    private async Task<AppointmentDto> LoadDtoAsync(Guid id)
    {
        var appt = await db.Appointments
            .Include(a => a.Customer).Include(a => a.Vehicle).Include(a => a.AssignedStaff)
            .FirstAsync(a => a.Id == id);
        return ToDto(appt);
    }

    public static AppointmentDto ToDto(Appointments a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        CustomerName = a.Customer?.FullName ?? "",
        VehicleId = a.VehicleId,
        VehicleNumber = a.Vehicle?.VehicleNumber ?? "",
        ServiceType = a.ServiceType,
        RequestedAt = a.RequestedAt,
        ConfirmedAt = a.ConfirmedAt,
        Status = a.Status.ToString(),
        Notes = a.Notes,
        AssignedStaffUserId = a.AssignedStaffUserId,
        AssignedStaffName = a.AssignedStaff?.FullName,
        CreatedAt = a.CreatedAt
    };
}
