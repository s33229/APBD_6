using APBD_6.DTOs;

namespace APBD_6.Services;

public interface IAppointmentsService
{
  int AddAppointment();
  void DeleteAppointment();
  IEnumerable<AppointmentListDto> GetAllAppointments();
  AppointmentDetailsDto GetAppointmentById(int id);
  void UpdateAppointment(int id, CreateAppointmentRequestDto appointment);
}