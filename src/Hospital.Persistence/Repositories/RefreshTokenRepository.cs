using Hospital.Domain.Entities.Identity;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    /// <summary>
    /// Concrete implementation of IRefreshTokenRepository using EF Core.
    /// 
    /// Notice it does NOT extend Repository&lt;T&gt; — RefreshToken has special queries
    /// that need to load the related ApplicationUser (for generating a new JWT).
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public RefreshTokenRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Finds an active (non-revoked, non-expired) refresh token.
        /// Includes the User so AuthService can generate a new JWT with the user's claims.
        /// </summary>
        public async Task<RefreshToken?> GetActiveTokenAsync(string token)
        {
            // We include the User navigation property because AuthService needs
            // user.Id, user.Email, and user.Roles to generate the new JWT.
            return await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        /// <summary>
        /// Adds a new refresh token to the change tracker.
        /// Does NOT save to DB — call SaveChangesAsync() after.
        /// </summary>
        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken);
        }

        /// <summary>
        /// Commits all tracked changes to the database.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
