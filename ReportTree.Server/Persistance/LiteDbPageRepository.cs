using ReportTree.Server.Models;
using LiteDB;

namespace ReportTree.Server.Persistance
{
    public class LiteDbPageRepository : IPageRepository
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<Page> _pages;
        
        public LiteDbPageRepository(LiteDatabase db) 
        { 
            _db = db;
            _pages = _db.GetCollection<Page>("pages");
            
            // Create indexes for performance
            _pages.EnsureIndex(x => x.ParentId);
            _pages.EnsureIndex(x => x.IsPublic);
        }

        public Task<IEnumerable<Page>> GetAllAsync()
        {
            return Task.FromResult(_pages.FindAll());
        }

        public Task<Page?> GetByIdAsync(int id)
        {
            return Task.FromResult<Page?>(_pages.FindById(id));
        }

        public Task<int> CreateAsync(Page page)
        {
            var id = _pages.Insert(page);
            return Task.FromResult(id.AsInt32);
        }

        public Task UpdateAsync(Page page)
        {
            _pages.Update(page);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _pages.Delete(id);
            return Task.CompletedTask;
        }
    }
}
