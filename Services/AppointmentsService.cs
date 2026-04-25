using System.Text;
using APBD_6.DTOs;
using APBD_6.Exceptions;
using Microsoft.Data.SqlClient;

namespace APBD_6.Services;

public class AppointmentsService(IConfiguration configuration) : IAppointmentsService
{
  private readonly string _connectionString = configuration.GetConnectionString("Default") ?? throw new ConnectionStringNotFoundException("Connection string not found exception");

  public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName, CancellationToken cancellationToken)
  {
    var result = new List<AppointmentListDto>();

    var sqlCommand = new StringBuilder("""
      SELECT
        a.IdAppointment,
        a.AppointmentDate,
        a.Status,
        a.Reason,
        p.FirstName + N' ' + p.LastName AS PatientFullName,
        p.Email AS PatientEmail
      FROM dbo.Appointments a
      JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
    """);
    var conditions = new List<string>();
    var parameters = new List<SqlParameter>();

    if (status is not null)
    {
      conditions.Add("status = @Status");
      parameters.Add(new SqlParameter("@Status", status));
    }

    if (patientLastName is not null)
    {
      conditions.Add("LastName = @PatientLastName");
      parameters.Add(new SqlParameter("@PatientLastName", patientLastName));
    }

    if (parameters.Count > 0)
    {
      sqlCommand.Append(" WHERE ");
      sqlCommand.Append(string.Join(" AND ", conditions));
    }

    sqlCommand.Append(" ORDER BY a.AppointmentDate;");

    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand();

    command.Connection = connection;
    command.CommandText = sqlCommand.ToString();
    command.Parameters.AddRange(parameters.ToArray());

    await connection.OpenAsync(cancellationToken);

    var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
      result.Add(new AppointmentListDto
      {
        IdAppointment = reader.GetInt32(0),
        AppointmentDate = reader.GetDateTime(1),
        Status = reader.GetString(2),
        Reason = reader.GetString(3),
        PatientFullName = reader.GetString(4),
        PatientEmail = reader.GetString(5)
      });
    }

    return result;
  }

  public async Task<AppointmentDetailsDto> GetAppointmentById(int id, CancellationToken cancellationToken)
  {
    var sqlCommand = new StringBuilder("""
      SELECT 
        a.IdAppointment AS AppointmentId,
        a.AppointmentDate,
        a.Status,
        a.Reason,
        a.InternalNotes,
        a.CreatedAt,
        p.FirstName + N' ' + p.LastName AS PatientFullName,
        p.Email AS PatientEmail,
        p.PhoneNumber AS PatientPhone,
        d.FirstName + N' ' + d.LastName AS DoctorFullName,
        d.LicenseNumber AS DoctorLicenseNumber
      FROM 
        dbo.Appointments a
      JOIN 
        dbo.Patients p ON a.IdPatient = p.IdPatient
      JOIN 
        dbo.Doctors d ON a.IdDoctor = d.IdDoctor
      WHERE 
        a.IdAppointment = @AppointmentId;
    """);
    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand();

    command.Connection = connection;
    command.CommandText = sqlCommand.ToString();
    command.Parameters.Add(new SqlParameter("@AppointmentId", id));

    await connection.OpenAsync(cancellationToken);

    var reader = await command.ExecuteReaderAsync(cancellationToken);

    AppointmentDetailsDto? result = null;
    while (await reader.ReadAsync(cancellationToken))
    {
      result ??= new AppointmentDetailsDto
      {
        IdAppointment = reader.GetInt32(0),
        AppointmentDate = reader.GetDateTime(1),
        Status = reader.GetString(2),
        Reason = reader.GetString(3),
        InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
        CreatedAt = reader.GetDateTime(5),
        PatientFullName = reader.GetString(6),
        PatientEmail = reader.GetString(7),
        PatientPhone = reader.GetString(8),
        DoctorFullName = reader.GetString(9),
        DoctorLicenseNumber = reader.GetString(10),
      };
    }

    if (result is null) throw new AppointmentNotFoundException($"Appointment with ID {id} not found");

    return result;
  }

  public async Task<AppointmentDetailsDto> AddAppointment(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {

    if (appointment.AppointmentDate < DateTime.Now)
    {
      throw new InvalidDateException("The appointment date cannot be in the past");
    }

    if (string.IsNullOrWhiteSpace(appointment.Reason))
    {
      throw new AppointmentReasonEmptyException("The appointment reason cannot be empty");
    }

    if (appointment.Reason.Length > 250)
    {
      throw new InvalidReasonException("The appointment reason cannot exceed 250 characters");
    }


    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand();

    await connection.OpenAsync(cancellationToken);

    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    command.Connection = connection;
    command.Transaction = (SqlTransaction)transaction;

    try
    {
      command.CommandText = "SELECT FirstName + ' ' + LastName, Email, PhoneNumber, IsActive FROM Patients WHERE IdPatient = @IdPatient";
      command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);

      await using var patientReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await patientReader.ReadAsync(cancellationToken))
      {
        throw new PatientDoesNotExistException($"Patient with ID {appointment.IdPatient} not found");
      }

      string patientFullName = patientReader.GetString(0);
      string patientEmail = patientReader.GetString(1);
      string patientPhone = patientReader.GetString(2);
      bool isPatientActive = patientReader.GetBoolean(3);

      if (!isPatientActive)
      {
        throw new PatientNotActiveException($"Patient with ID {appointment.IdPatient} is not active");
      }

      await patientReader.CloseAsync();
      command.Parameters.Clear();


      command.CommandText = "SELECT FirstName + ' ' + LastName, LicenseNumber, IsActive FROM Doctors WHERE IdDoctor = @IdDoctor";
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);

      await using var doctorReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await doctorReader.ReadAsync(cancellationToken))
      {
        throw new DoctorDoesNotExistException($"Doctor with ID {appointment.IdDoctor} not found");
      }

      var doctorFullName = doctorReader.GetString(0);
      var doctorLicenseNumber = doctorReader.GetString(1);
      bool isDoctorActive = doctorReader.GetBoolean(2);

      if (!isDoctorActive)
      {
        throw new DoctorNotActiveException($"Doctor with ID {appointment.IdDoctor} is not active");
      }


      await doctorReader.CloseAsync();
      command.Parameters.Clear();


      command.CommandText = "SELECT 1 FROM Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate AND Status != 'Cancelled'";
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
      command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);

      var conflictExists = await command.ExecuteScalarAsync(cancellationToken);
      if (conflictExists is not null)
      {
        throw new DateConflictException($"Doctor already has an appointment scheduled at {appointment.AppointmentDate}");
      }
      command.Parameters.Clear();


      var status = "Scheduled";
      var createdAt = DateTime.Now;

      command.CommandText = """
                              INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Reason, Status, CreatedAt, InternalNotes)
                              OUTPUT inserted.IdAppointment
                              VALUES (@IdPatient, @IdDoctor, @AppointmentDate, @Reason, @Status, @CreatedAt, '')
                              """;

      command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
      command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
      command.Parameters.AddWithValue("@Reason", appointment.Reason);
      command.Parameters.AddWithValue("@Status", status);
      command.Parameters.AddWithValue("@CreatedAt", createdAt);

      var appointmentId = await command.ExecuteScalarAsync(cancellationToken);
      command.Parameters.Clear();

      await transaction.CommitAsync(cancellationToken);

      return new AppointmentDetailsDto
      {
        IdAppointment = (int)appointmentId!,
        AppointmentDate = appointment.AppointmentDate,
        Status = status,
        Reason = appointment.Reason,
        InternalNotes = "",
        CreatedAt = createdAt,
        PatientFullName = patientFullName,
        PatientEmail = patientEmail,
        PatientPhone = patientPhone,
        DoctorFullName = doctorFullName,
        DoctorLicenseNumber = doctorLicenseNumber
      };
    }
    catch (Exception)
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }

  public async Task DeleteAppointment(int id, CancellationToken cancellationToken)
  {
    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand();

    await connection.OpenAsync(cancellationToken);

    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    command.Connection = connection;
    command.Transaction = (SqlTransaction)transaction;

    try
    {
      command.CommandText = "SELECT Status FROM Appointments WHERE IdAppointment = @IdAppointment";
      command.Parameters.AddWithValue("@IdAppointment", id);

      await using var appointmentReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await appointmentReader.ReadAsync(cancellationToken))
      {
        throw new AppointmentNotFoundException($"Appointment with ID {id} not found");
      }

      string appointmentStatus = appointmentReader.GetString(0);

      if (appointmentStatus == "Completed")
      {
        throw new AppointmentAlreadyCompletedException($"Appointment already completed");
      }

      await appointmentReader.CloseAsync();
      command.Parameters.Clear();

      command.CommandText = "DELETE FROM Appointments WHERE IdAppointment = @IdAppointment";
      command.Parameters.AddWithValue("@IdAppointment", id);

      var appointmentId = await command.ExecuteScalarAsync(cancellationToken);
      command.Parameters.Clear();

      await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception)
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }

  public async Task UpdateAppointment(int id, UpdateAppointmentRequestDto appointment, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(appointment.Reason))
    {
      throw new AppointmentReasonEmptyException("The appointment reason cannot be empty");
    }

    if (appointment.Reason.Length > 250)
    {
      throw new InvalidReasonException("The appointment reason cannot exceed 250 characters");
    }

    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand();

    await connection.OpenAsync(cancellationToken);

    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    command.Connection = connection;
    command.Transaction = (SqlTransaction)transaction;

    try
    {
      command.CommandText = "SELECT Status, AppointmentDate FROM Appointments WHERE IdAppointment = @IdAppointment";
      command.Parameters.AddWithValue("@IdAppointment", id);

      await using var appointmentReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await appointmentReader.ReadAsync(cancellationToken))
      {
        throw new AppointmentNotFoundException($"Appointment with ID {id} not found");
      }

      string appointmentStatus = appointmentReader.GetString(0);
      DateTime appointmentDate = appointmentReader.GetDateTime(1);

      if (appointmentStatus == "Completed" && appointmentDate != appointment.AppointmentDate)
      {
        throw new AppointmentAlreadyCompletedException($"Appointment already completed");
      }

      await appointmentReader.CloseAsync();
      command.Parameters.Clear();

      command.CommandText = "SELECT IsActive FROM Patients WHERE IdPatient = @IdPatient";
      command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);

      await using var patientReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await patientReader.ReadAsync(cancellationToken))
      {
        throw new PatientDoesNotExistException($"Patient with ID {appointment.IdPatient} not found");
      }

      bool isPatientActive = patientReader.GetBoolean(0);

      if (!isPatientActive)
      {
        throw new PatientNotActiveException($"Patient with ID {appointment.IdPatient} is not active");
      }

      await patientReader.CloseAsync();
      command.Parameters.Clear();

      command.CommandText = "SELECT IsActive FROM Doctors WHERE IdDoctor = @IdDoctor";
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);

      await using var doctorReader = await command.ExecuteReaderAsync(cancellationToken);
      if (!await doctorReader.ReadAsync(cancellationToken))
      {
        throw new DoctorDoesNotExistException($"Doctor with ID {appointment.IdDoctor} not found");
      }

      bool isDoctorActive = doctorReader.GetBoolean(0);

      if (!isDoctorActive)
      {
        throw new DoctorNotActiveException($"Doctor with ID {appointment.IdDoctor} is not active");
      }

      await doctorReader.CloseAsync();
      command.Parameters.Clear();

      command.CommandText = "SELECT 1 FROM Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate AND Status != 'Cancelled'";
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
      command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);

      var conflictExists = await command.ExecuteScalarAsync(cancellationToken);
      if (conflictExists is not null && appointmentStatus == appointment.Status)
      {
        throw new DateConflictException($"Doctor already has an appointment scheduled at {appointment.AppointmentDate}");
      }
      command.Parameters.Clear();

      command.CommandText = """
                              UPDATE Appointments
                                SET
                                  IdPatient = @IdPatient, 
                                  IdDoctor = @IdDoctor, 
                                  AppointmentDate = @AppointmentDate, 
                                  Reason = @Reason, 
                                  Status = @Status, 
                                  InternalNotes = @InternalNotes
                                WHERE IdAppointment = @IdAppointment
                              """;

      command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
      command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
      command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
      command.Parameters.AddWithValue("@Reason", appointment.Reason);
      command.Parameters.AddWithValue("@Status", appointment.Status);
      command.Parameters.AddWithValue("@InternalNotes", appointment.InternalNotes);
      command.Parameters.AddWithValue("@IdAppointment", id);

      var appointmentId = await command.ExecuteScalarAsync(cancellationToken);
      command.Parameters.Clear();

      await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception)
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }
}