using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public class LiteDbUserRepository : IUserRepository
    {
        private readonly LiteDB.LiteDatabase _db;
        public LiteDbUserRepository(LiteDB.LiteDatabase db) { _db = db; }

        public void Upsert(AppUser user, string plainPassword)
        {
            var col = _db.GetCollection<AppUser>("users");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            col.Upsert(user);
        }

        public AppUser? Validate(string username, string plainPassword)
        {
            var col = _db.GetCollection<AppUser>("users");
            var user = col.FindOne(x => x.Username == username);
            if (user == null) return null;
            return BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash) ? user : null;
        }
    }
}
