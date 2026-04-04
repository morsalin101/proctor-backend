using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ProctorDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
