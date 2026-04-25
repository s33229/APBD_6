using APBD_6.DTOs;

namespace APBD_6.Services;

public interface IAppointmentsService
{
  public Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName, CancellationToken cancellationToken);
  public Task<AppointmentDetailsDto> GetAppointmentById(int id, CancellationToken cancellationToken);
  public Task<AppointmentDetailsDto> AddAppointment(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken);
  public Task UpdateAppointment(int id, UpdateAppointmentRequestDto appointment, CancellationToken cancellationToken);
  public Task DeleteAppointment(int id, CancellationToken cancellationToken);
}