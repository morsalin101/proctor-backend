namespace PROCTOR.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICaseRepository Cases { get; }
    IHearingRepository Hearings { get; }
    Task<int> SaveChangesAsync();
}
