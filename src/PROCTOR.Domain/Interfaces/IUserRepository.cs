using PROCTOR.Domain.Entities;

namespace PROCTOR.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
