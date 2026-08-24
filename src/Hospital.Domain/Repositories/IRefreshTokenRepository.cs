using Hospital.Domain.Entities.Identity;
using System;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Repository for managing refresh tokens.
    /// 
    /// Why is this in Domain and not handled by AuthService directly?
    /// Clean Architecture says: the Domain layer defines WHAT data operations exist.
    /// The Persistence layer implements HOW they talk to the database.
    /// Infrastructure (AuthService) only knows about the interface, not the database.
    /// 
    /// This keeps AuthService testable — you can mock IRefreshTokenRepository
    /// without needing a real database.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Finds an active refresh token string and returns it with the associated user loaded.
        /// </summary>
        Task<RefreshToken?> GetActiveTokenAsync(string token);

        /// <summary>
        /// Adds a new refresh token to the database.
        /// </summary>
        Task AddAsync(RefreshToken refreshToken);

        /// <summary>
        /// Saves all pending changes.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
