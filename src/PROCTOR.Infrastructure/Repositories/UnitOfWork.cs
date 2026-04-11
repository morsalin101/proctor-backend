using PROCTOR.Domain.Interfaces;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ProctorDbContext _context;
    private IUserRepository? _users;
    private ICaseRepository? _cases;
    private IHearingRepository? _hearings;

    public UnitOfWork(ProctorDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ICaseRepository Cases => _cases ??= new CaseRepository(_context);
    public IHearingRepository Hearings => _hearings ??= new HearingRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Add<T>(T entity) where T : class
    {
        _context.Set<T>().Add(entity);
    }

    public void Remove<T>(T entity) where T : class
    {
        _context.Set<T>().Remove(entity);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
