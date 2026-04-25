
using APBD_6.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APBD_6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{

  [HttpGet]
  public ActionResult<AppointmentListDto> GetAppointments(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  [HttpGet("{id}")]
  public ActionResult<AppointmentDetailsDto> GetAppointmentsById([FromRoute] int id, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  [HttpPost]
  public IActionResult AddAppointment([FromBody] CreateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  [HttpPut("{id}")]
  public IActionResult UpdateAppointment([FromRoute] int id, [FromBody] UpdateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  [HttpDelete("{id}")]
  public IActionResult DeleteAppointment([FromRoute] int id, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

}