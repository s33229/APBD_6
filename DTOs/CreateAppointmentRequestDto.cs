using System.ComponentModel.DataAnnotations;

namespace APBD_6.DTOs;

public class CreateAppointmentRequestDto
{
  [Required(ErrorMessage = "Patient ID is required")]
  public int IdPatient { get; set; }

  [Required(ErrorMessage = "Doctor ID is required")]
  public int IdDoctor { get; set; }

  [Required(ErrorMessage = "Appointment date is required")]
  public DateTime AppointmentDate { get; set; }

  [Required(ErrorMessage = "Appointment reason is required")]
  [MaxLength(250, ErrorMessage = "Appointment reason cannot exceed 250 characters")]
  public string Reason { get; set; } = string.Empty;
}