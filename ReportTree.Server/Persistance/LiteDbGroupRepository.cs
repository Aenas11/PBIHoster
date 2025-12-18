using ReportTree.Server.Models;
using LiteDB;

namespace ReportTree.Server.Persistance
{
    public class LiteDbGroupRepository : IGroupRepository
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<Group> _groups;
        
        public LiteDbGroupRepository(LiteDatabase db) 
        { 
            _db = db;
            _groups = _db.GetCollection<Group>("groups");
            
            // Create indexes for performance
            _groups.EnsureIndex(x => x.Name);
        }

        public Task<IEnumerable<Group>> GetAllAsync()
        {
            return Task.FromResult(_groups.FindAll());
        }

        public Task<IEnumerable<Group>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Task.FromResult(_groups.FindAll());
            
            var results = _groups.Find(x => x.Name.Contains(term));
            return Task.FromResult(results);
        }

        public Task<int> CreateAsync(Group group)
        {
            var id = _groups.Insert(group);
            return Task.FromResult(id.AsInt32);
        }

        public Task UpdateAsync(Group group)
        {
            _groups.Update(group);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _groups.Delete(id);
            return Task.CompletedTask;
        }
    }
}
