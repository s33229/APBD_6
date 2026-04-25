
using APBD_6.DTOs;
using APBD_6.Exceptions;
using APBD_6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace APBD_6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentsService service) : ControllerBase
{

  [HttpGet]
  public async Task<ActionResult<AppointmentListDto>> GetAppointments(
    [FromQuery] string? status,
    [FromQuery] string? patientLastName,
    CancellationToken cancellationToken)
  {
    return Ok(await service.GetAllAppointmentsAsync(status, patientLastName, cancellationToken));
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<AppointmentDetailsDto>> GetAppointmentsById([FromRoute] int id, CancellationToken cancellationToken)
  {
    try
    {
      return Ok(await service.GetAppointmentById(id, cancellationToken));
    }
    catch (AppointmentNotFoundException e)
    {
      return NotFound(e.Message);
    }
    catch (Exception)
    {
      return Problem("Internal server error");
    }
  }

  [HttpPost]
  public async Task<IActionResult> AddAppointment([FromBody] CreateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {
    try
    {
      var result = await service.AddAppointment(appointment, cancellationToken);

      return CreatedAtAction(
         actionName: nameof(GetAppointmentsById),
         routeValues: new { id = result.IdAppointment },
         value: result
     );
    }
    catch (InvalidDateException e)
    {
      return BadRequest(e.Message);
    }
    catch (AppointmentReasonEmptyException e)
    {
      return BadRequest(e.Message);
    }
    catch (InvalidReasonException e)
    {
      return BadRequest(e.Message);
    }
    catch (PatientDoesNotExistException e)
    {
      return BadRequest(e.Message);
    }
    catch (PatientNotActiveException e)
    {
      return BadRequest(e.Message);
    }
    catch (DoctorDoesNotExistException e)
    {
      return BadRequest(e.Message);
    }
    catch (DoctorNotActiveException e)
    {
      return BadRequest(e.Message);
    }
    catch (DateConflictException e)
    {
      return Conflict(e.Message);
    }
    catch (Exception)
    {
      return Problem("Internal server error");
    }
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteAppointment([FromRoute] int id, CancellationToken cancellationToken)
  {
    try
    {
      await service.DeleteAppointment(id, cancellationToken);
      return NoContent();
    }
    catch (AppointmentNotFoundException e)
    {
      return NotFound(e.Message);
    }
    catch (AppointmentAlreadyCompletedException e)
    {
      return Conflict(e.Message);
    }
    catch (Exception)
    {
      return Problem("Internal server error");
    }
  }


  [HttpPut("{id}")]
  public async Task<IActionResult> UpdateAppointment([FromRoute] int id, [FromBody] UpdateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {
    try
    {
      await service.UpdateAppointment(id, appointment, cancellationToken);
      return Ok();
    }
    catch (AppointmentReasonEmptyException e)
    {
      return BadRequest(e.Message);
    }
    catch (InvalidReasonException e)
    {
      return BadRequest(e.Message);
    }
    catch (AppointmentNotFoundException e)
    {
      return NotFound(e.Message);
    }
    catch (AppointmentAlreadyCompletedException e)
    {
      return Conflict(e.Message);
    }
    catch (PatientDoesNotExistException e)
    {
      return BadRequest(e.Message);
    }
    catch (PatientNotActiveException e)
    {
      return BadRequest(e.Message);
    }
    catch (DoctorDoesNotExistException e)
    {
      return BadRequest(e.Message);
    }
    catch (DoctorNotActiveException e)
    {
      return BadRequest(e.Message);
    }
    catch (DateConflictException e)
    {
      return Conflict(e.Message);
    }
    catch (Exception)
    {
      return Problem("Internal server error");
    }
  }

}