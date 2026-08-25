using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Appointment-specific repository interface.
    ///
    /// Appointments have two foreign keys (PatientId, DoctorId).
    /// We MUST include those navigation properties before mapping to DTO,
    /// otherwise AutoMapper throws NullReferenceException reading Patient.FirstName.
    /// </summary>
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        /// <summary>
        /// Returns all appointments with Patient and Doctor loaded.
        /// Use for simple internal operations; prefer GetPagedAsync for API endpoints.
        /// </summary>
        Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync();

        /// <summary>
        /// Returns a single appointment with Patient and Doctor loaded by id.
        /// </summary>
        Task<Appointment?> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// Returns a paginated, filtered page of appointments with Patient and Doctor loaded.
        /// Supports filtering by patientId, doctorId, status, and date range.
        /// </summary>
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(AppointmentQueryParams queryParams);
    }
}
