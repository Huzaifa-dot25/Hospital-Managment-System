using Hospital.Domain.Entities;

namespace Hospital.Domain.Repositories
{
    public interface IPatientRepository : IRepository<Patient>
    {
        // Add specific methods for Patient that are not in the generic IRepository
        // For example:
        // Task<Patient?> GetPatientWithMedicalHistoryAsync(Guid patientId);
    }
}
