using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Models.Entities;

namespace SoftreserveTracker.Web.Services.Rosters;

public interface IRosterService
{
    Task<Roster?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default);
    Task<Roster> CreateAsync(string name, CancellationToken cancellationToken = default);
}

public sealed class RosterService(AppDbContext db) : IRosterService
{
    public Task<Roster?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default)
    {
        return db.Rosters.FirstOrDefaultAsync(r => r.AccessToken == accessToken, cancellationToken);
    }

    public async Task<Roster> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var roster = new Roster
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            AccessToken = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        db.Rosters.Add(roster);
        await db.SaveChangesAsync(cancellationToken);
        return roster;
    }
}
