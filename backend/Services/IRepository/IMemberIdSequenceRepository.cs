namespace RB_Website_API.Services.IRepository;

public interface IMemberIdSequenceRepository
{
    /// <summary>Atomically increments and returns the next sequence number for the given calendar year.</summary>
    Task<int> GetNextSequenceNumberAsync(int year, CancellationToken ct);
}
