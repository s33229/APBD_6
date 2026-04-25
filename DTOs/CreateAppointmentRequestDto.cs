using System.ComponentModel.DataAnnotations;

namespace APBD_6.DTOs;

public class CreateAppointmentRequestDto
{
  [Required(ErrorMessage = "ID pacjenta jest wymagane.")]
  public int IdPatient { get; set; }

  [Required(ErrorMessage = "ID lekarza jest wymagane.")]
  public int IdDoctor { get; set; }

  [Required(ErrorMessage = "Data wizyty jest wymagana.")]
  public DateTime AppointmentDate { get; set; }

  [Required(ErrorMessage = "Powód wizyty jest wymagany.")]
  [MaxLength(250, ErrorMessage = "Powód wizyty nie może być dłuższy niż 250 znaków.")]
  public string Reason { get; set; } = string.Empty;
}