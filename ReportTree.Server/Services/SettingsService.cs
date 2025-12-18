using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using System.Security.Cryptography;
using System.Text;

namespace ReportTree.Server.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repo;
    private readonly string _encryptionKey;

    public SettingsService(ISettingsRepository repo, IConfiguration config)
    {
        _repo = repo;
        _encryptionKey = config["Encryption:Key"] ?? "default-encryption-key-change-in-production-must-be-32-chars!";
    }

    public async Task<AppSetting?> GetSettingAsync(string key)
    {
        var setting = await _repo.GetByKeyAsync(key);
        if (setting != null && setting.IsEncrypted)
        {
            setting.Value = Decrypt(setting.Value);
        }
        return setting;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await GetSettingAsync(key);
        return setting?.Value;
    }

    public async Task<IEnumerable<AppSetting>> GetAllSettingsAsync()
    {
        var settings = await _repo.GetAllAsync();
        // Don't decrypt values when returning all settings for security
        return settings;
    }

    public async Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        return await _repo.GetByCategoryAsync(category);
    }

    public async Task UpsertSettingAsync(string key, string value, string category, string description, bool isEncrypted, string modifiedBy)
    {
        var setting = new AppSetting
        {
            Key = key,
            Value = isEncrypted ? Encrypt(value) : value,
            Category = category,
            Description = description,
            IsEncrypted = isEncrypted,
            ModifiedBy = modifiedBy,
            LastModified = DateTime.UtcNow
        };
        await _repo.UpsertAsync(setting);
    }

    public async Task DeleteSettingAsync(string key)
    {
        await _repo.DeleteAsync(key);
    }

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_encryptionKey);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipherText)
    {
        try
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = DeriveKey(_encryptionKey);

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[buffer.Length - iv.Length];
            Array.Copy(buffer, iv, iv.Length);
            Array.Copy(buffer, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            return cipherText; // Return as-is if decryption fails
        }
    }

    private byte[] DeriveKey(string key)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    public async Task InitializeDefaultSettingsAsync()
    {
        // Initialize default static app settings if they don't exist
        var homePageId = await GetSettingAsync("App.HomePageId");
        if (homePageId == null)
        {
            await UpsertSettingAsync(
                "App.HomePageId",
                "",
                "Application",
                "The page ID to display on the home route (/)",
                false,
                "System"
            );
        }
    }
}
