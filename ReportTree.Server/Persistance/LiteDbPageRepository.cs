using ReportTree.Server.Models;
using LiteDB;

namespace ReportTree.Server.Persistance
{
    public class LiteDbPageRepository : IPageRepository
    {
        private readonly LiteDatabase _db;
        public LiteDbPageRepository(LiteDatabase db) { _db = db; }

        public Task<IEnumerable<Page>> GetAllAsync()
        {
            var col = _db.GetCollection<Page>("pages");
            return Task.FromResult(col.FindAll());
        }

        public Task<Page?> GetByIdAsync(int id)
        {
            var col = _db.GetCollection<Page>("pages");
            return Task.FromResult<Page?>(col.FindById(id));
        }

        public Task<int> CreateAsync(Page page)
        {
            var col = _db.GetCollection<Page>("pages");
            var id = col.Insert(page);
            return Task.FromResult(id.AsInt32);
        }

        public Task UpdateAsync(Page page)
        {
            var col = _db.GetCollection<Page>("pages");
            col.Update(page);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var col = _db.GetCollection<Page>("pages");
            col.Delete(id);
            return Task.CompletedTask;
        }
    }
}
