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
        private IMedicalRecordRepository? _medicalRecordRepository;
        private IMedicationRepository? _medicationRepository;
        private IPrescriptionRepository? _prescriptionRepository;
        private ILabTestRepository? _labTestRepository;
        private ILabOrderRepository? _labOrderRepository;
        private bool _disposed;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public IPatientRepository Patients => _patientRepository ??= new PatientRepository(_dbContext);

        public IDoctorRepository Doctors => _doctorRepository ??= new DoctorRepository(_dbContext);

        public IDepartmentRepository Departments => _departmentRepository ??= new DepartmentRepository(_dbContext);

        public IAppointmentRepository Appointments => _appointmentRepository ??= new AppointmentRepository(_dbContext);

        public IMedicalRecordRepository MedicalRecords => _medicalRecordRepository ??= new MedicalRecordRepository(_dbContext);

        public IMedicationRepository Medications => _medicationRepository ??= new MedicationRepository(_dbContext);

        public IPrescriptionRepository Prescriptions => _prescriptionRepository ??= new PrescriptionRepository(_dbContext);

        public ILabTestRepository LabTests => _labTestRepository ??= new LabTestRepository(_dbContext);

        public ILabOrderRepository LabOrders => _labOrderRepository ??= new LabOrderRepository(_dbContext);

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
