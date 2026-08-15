using System.Security.Cryptography;
using System.Text;

namespace SessionScape.Server.Simulation
{
    public class PlayerAccountRepository
    {
        private const int SALT_SIZE = 16;
        private const int HASH_SIZE = 32;
        private const int Iterations = 100_000;
        
        private readonly string _accountsDirectory;

        public PlayerAccountRepository(string accountsDirectory)
        {
            _accountsDirectory = accountsDirectory;
            Directory.CreateDirectory(_accountsDirectory);
        }

        private string GetPath(string name) => Path.Combine(_accountsDirectory, $"{name.ToLowerInvariant()}.acct");

        public bool TryLoad(string name, out PlayerAccount account)
        {
            account = null;

            string path = GetPath(name);

            if (!File.Exists(path))
                return false;

            try
            {
                byte[] data = File.ReadAllBytes(path);
                account = AccountSerializer.Deserialize(data);

                return true;
            }
            catch
            {
                account = null;
                return false;
            }
        }

        public void Save(PlayerAccount account)
        {
            byte[] data = AccountSerializer.Serialize(account);

            string path = GetPath(account.Name);

            File.WriteAllBytes(path, data);
        }

        public PlayerAccount CreateAccount(string name, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HASH_SIZE);

            PlayerAccount account = new()
            {
                Name = name,
                PasswordHash = hash,
                PasswordSalt = salt,

                X = 0,
                Y = 0f,
                Z = 0
            };

            Save(account);

            return account;
        }

        public bool VerifyPassword(PlayerAccount account, string password)
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), account.PasswordSalt, Iterations, HashAlgorithmName.SHA256, HASH_SIZE);

            return CryptographicOperations.FixedTimeEquals(hash, account.PasswordHash);
        }
    }
}
