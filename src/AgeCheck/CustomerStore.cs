using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgeCheck.Core;

namespace AgeCheck;

// DPAPI binds the encrypted file (including its random HMAC key) to this Windows
// account. Only the keyed fingerprint, name, visit count and timestamps persist.
internal sealed class CustomerStore
{
    private sealed record Archive(int Version, byte[] Key, CustomerRecord[] Customers);
    private readonly string path;
    private readonly bool memoryOnly;
    private byte[] key = RandomNumberGenerator.GetBytes(32);
    private CustomerBook book = new();
    public bool Available { get; private set; } = true;
    public string? Error { get; private set; }
    public IReadOnlyList<CustomerRecord> Records => book.Records;

    public CustomerStore(string path, bool memoryOnly = false)
    {
        this.path = path; this.memoryOnly = memoryOnly;
        try { if (!memoryOnly) Load(); }
        catch (Exception e) when (StorageError(e)) { Fail("Saved customers could not be opened. Scanning still works. Delete saved customers to reset the local store."); }
    }
    private static bool StorageError(Exception e) => e is IOException or UnauthorizedAccessException or CryptographicException or JsonException or ArgumentException;
    private void Fail(string message) { Available = false; Error = message; }
    private void Load()
    {
        if (!File.Exists(path)) { book = new(); key = RandomNumberGenerator.GetBytes(32); return; }
        if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Customer file too large.");
        byte[] plaintext = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        try
        {
            var archive = JsonSerializer.Deserialize<Archive>(plaintext);
            if (archive is not { Version: 1, Key.Length: 32, Customers: not null } || archive.Customers.Length > CustomerBook.Capacity)
                throw new InvalidDataException("Unsupported customer file.");
            var loaded = new CustomerBook(archive.Customers);
            key = archive.Key; book = loaded;
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
    public CustomerVisit? Remember(CheckResult result, DateTimeOffset time)
    {
        if (!Available || result.Outcome == Outcome.Review || result.CardIdentity == null || result.DisplayName == null) return null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var gate = memoryOnly ? null : new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (!memoryOnly) Load(); // merge visits from other running copies under the same lock
            string fingerprint = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(result.CardIdentity)));
            var updated = new CustomerBook(book.Records);
            var visit = updated.Visit(fingerprint, result.DisplayName, time);
            if (!memoryOnly) Save(updated);
            book = updated; Error = null;
            return visit;
        }
        catch (Exception e) when (StorageError(e))
        { Error = "This scan was checked, but its customer record could not be saved. Try again later or reset saved customers in Settings."; return null; }
    }
    private void Save(CustomerBook updated)
    {
        byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(new Archive(1, key, updated.Records.ToArray()));
        byte[] encrypted;
        try { encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, encrypted); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public bool DeleteAll()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var gate = memoryOnly ? null : new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (!memoryOnly) File.Delete(path);
            book = new(); key = RandomNumberGenerator.GetBytes(32); Available = true; Error = null;
            return true;
        }
        catch (Exception e) when (StorageError(e)) { Error = "Saved customers could not be deleted. Check folder permissions and close other copies of AgeCheck."; return false; }
    }
}
