using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;

namespace Hospital.Persistence.Repositories
{
    public class DepartmentRepository : Repository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
