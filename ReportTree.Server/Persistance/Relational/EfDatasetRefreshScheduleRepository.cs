using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfDatasetRefreshScheduleRepository : IDatasetRefreshScheduleRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfDatasetRefreshScheduleRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<DatasetRefreshSchedule>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshSchedules.ToListAsync();
    }

    public async Task<DatasetRefreshSchedule?> GetByIdAsync(Guid id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshSchedules.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task CreateAsync(DatasetRefreshSchedule schedule)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.RefreshSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DatasetRefreshSchedule schedule)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.RefreshSchedules.Update(schedule);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var schedule = await dbContext.RefreshSchedules.FirstOrDefaultAsync(x => x.Id == id);
        if (schedule == null)
        {
            return;
        }

        dbContext.RefreshSchedules.Remove(schedule);
        await dbContext.SaveChangesAsync();
    }
}