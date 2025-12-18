using ReportTree.Server.Models;
using LiteDB;

namespace ReportTree.Server.Persistance
{
    public class LiteDbGroupRepository : IGroupRepository
    {
        private readonly LiteDatabase _db;
        public LiteDbGroupRepository(LiteDatabase db) { _db = db; }

        public Task<IEnumerable<Group>> GetAllAsync()
        {
            var col = _db.GetCollection<Group>("groups");
            return Task.FromResult(col.FindAll());
        }

        public Task<IEnumerable<Group>> SearchAsync(string term)
        {
            var col = _db.GetCollection<Group>("groups");
            if (string.IsNullOrWhiteSpace(term)) return Task.FromResult(col.FindAll());
            
            var results = col.Find(x => x.Name.Contains(term));
            return Task.FromResult(results);
        }

        public Task<int> CreateAsync(Group group)
        {
            var col = _db.GetCollection<Group>("groups");
            var id = col.Insert(group);
            return Task.FromResult(id.AsInt32);
        }

        public Task DeleteAsync(int id)
        {
            var col = _db.GetCollection<Group>("groups");
            col.Delete(id);
            return Task.CompletedTask;
        }
    }
}
