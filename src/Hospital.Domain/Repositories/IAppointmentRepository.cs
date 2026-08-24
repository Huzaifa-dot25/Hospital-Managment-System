using Hospital.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Appointment-specific repository interface.
    /// Appointments need Patient and Doctor loaded to display names in the DTO.
    /// </summary>
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        /// <summary>
        /// Returns all appointments WITH Patient and Doctor navigation properties loaded.
        /// Without this, mapping PatientName and DoctorName in AutoMapper will throw NullReferenceException.
        /// </summary>
        Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync();

        /// <summary>
        /// Returns a single appointment WITH Patient and Doctor loaded.
        /// </summary>
        Task<Appointment?> GetByIdWithDetailsAsync(Guid id);
    }
}
