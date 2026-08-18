using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IPatientRepository Patients { get; }
        IDoctorRepository Doctors { get; }
        IDepartmentRepository Departments { get; }
        IAppointmentRepository Appointments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
