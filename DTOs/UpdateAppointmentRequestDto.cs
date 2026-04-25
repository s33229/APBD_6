using System.ComponentModel.DataAnnotations;

namespace APBD_6.DTOs;

public class UpdateAppointmentRequestDto
{
  [Required]
  public int IdPatient { get; set; }

  [Required]
  public int IdDoctor { get; set; }

  [Required]
  public DateTime AppointmentDate { get; set; }

  [Required]
  [RegularExpression("^(Scheduled|Completed|Cancelled)$", ErrorMessage = "Status musi być wartością: Scheduled, Completed lub Cancelled.")]
  public string Status { get; set; } = string.Empty;

  [Required]
  [MaxLength(250, ErrorMessage = "Powód wizyty nie może być dłuższy niż 250 znaków.")]
  public string Reason { get; set; } = string.Empty;

  public string InternalNotes { get; set; } = string.Empty;
}