using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public class LiteDbUserRepository : IUserRepository
    {
        private readonly LiteDB.LiteDatabase _db;
        public LiteDbUserRepository(LiteDB.LiteDatabase db) { _db = db; }

        public Task UpsertAsync(AppUser user)
        {
            var col = _db.GetCollection<AppUser>("users");
            col.Upsert(user);
            return Task.CompletedTask;
        }

        public Task<AppUser?> GetByUsernameAsync(string username)
        {
            var col = _db.GetCollection<AppUser>("users");
            var user = col.FindOne(x => x.Username == username);
            return Task.FromResult<AppUser?>(user);
        }
    }
}
