using ReportTree.Server.Models;
using LiteDB;

namespace ReportTree.Server.Persistance
{
    public class LiteDbUserRepository : IUserRepository
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<AppUser> _users;
        
        public LiteDbUserRepository(LiteDatabase db) 
        { 
            _db = db;
            _users = _db.GetCollection<AppUser>("users");
            
            // Create indexes for performance
            _users.EnsureIndex(x => x.Username);
        }

        public Task UpsertAsync(AppUser user)
        {
            _users.Upsert(user);
            return Task.CompletedTask;
        }

        public Task<AppUser?> GetByUsernameAsync(string username)
        {
            var user = _users.FindOne(x => x.Username == username);
            return Task.FromResult<AppUser?>(user);
        }

        public Task<IEnumerable<AppUser>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Task.FromResult(_users.FindAll());
            
            var results = _users.Find(x => x.Username.Contains(term));
            return Task.FromResult(results);
        }

        public Task DeleteAsync(string username)
        {
            var user = _users.FindOne(x => x.Username == username);
            if (user != null)
            {
                _users.Delete(user.Id);
            }
            return Task.CompletedTask;
        }
    }
}
