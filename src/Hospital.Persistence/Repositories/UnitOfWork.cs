using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IPatientRepository? _patientRepository;
        private IDoctorRepository? _doctorRepository;
        private IDepartmentRepository? _departmentRepository;
        private IAppointmentRepository? _appointmentRepository;
        private bool _disposed;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public IPatientRepository Patients => _patientRepository ??= new PatientRepository(_dbContext);

        public IDoctorRepository Doctors => _doctorRepository ??= new DoctorRepository(_dbContext);

        public IDepartmentRepository Departments => _departmentRepository ??= new DepartmentRepository(_dbContext);

        public IAppointmentRepository Appointments => _appointmentRepository ??= new AppointmentRepository(_dbContext);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
