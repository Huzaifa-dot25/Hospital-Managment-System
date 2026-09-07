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
        IMedicalRecordRepository MedicalRecords { get; }
        IMedicationRepository Medications { get; }
        IPrescriptionRepository Prescriptions { get; }
        ILabTestRepository LabTests { get; }
        ILabOrderRepository LabOrders { get; }
        IInvoiceRepository Invoices { get; }
        IPaymentRepository Payments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
