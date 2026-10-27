using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConfigBackupTool.Credentials
{
    // Хранит пароль, зашифрованный через Windows DPAPI (DataProtectionScope.CurrentUser):
    // расшифровать его может только тот же пользователь Windows на этом же ПК.
    // Файл лежит в %LOCALAPPDATA%\ConfigBackupTool\credentials.dat — НЕ рядом с exe,
    // чтобы случайно не уехать на флешку или в git вместе с программой.
    public class DpapiCredentialStore : ICredentialStore
    {
        // Доп. "соль" DPAPI: блоб, созданный другим приложением, этим хранилищем не расшифруется.
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("ConfigBackupTool.RememberedPassword.v1");

        private readonly string _filePath;

        public DpapiCredentialStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ConfigBackupTool",
                "credentials.dat"))
        {
        }

        public DpapiCredentialStore(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            _filePath = filePath;
        }

        public bool HasPassword()
        {
            return Load() != null;
        }

        public string Load()
        {
            if (File.Exists(_filePath) == false)
            {
                return null;
            }

            byte[] protectedBytes;

            try
            {
                protectedBytes = File.ReadAllBytes(_filePath);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            if (protectedBytes.Length == 0)
            {
                return null;
            }

            byte[] plainBytes;

            try
            {
                plainBytes = ProtectedData.Unprotect(protectedBytes, _entropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                // Файл от другого пользователя/ПК или повреждён — считаем, что пароля нет.
                return null;
            }

            try
            {
                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }

        public void Save(string password)
        {
            if (string.IsNullOrEmpty(password) == true)
            {
                throw new ArgumentException("Пароль не может быть пустым.", "password");
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(password);
            byte[] protectedBytes;

            try
            {
                protectedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }

            string directory = Path.GetDirectoryName(_filePath);

            if (string.IsNullOrEmpty(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            // Пишем во временный файл и подменяем — чтобы сбой посреди записи
            // не оставил пустой/битый credentials.dat вместо рабочего.
            string tempPath = _filePath + ".tmp";
            File.WriteAllBytes(tempPath, protectedBytes);
            File.Move(tempPath, _filePath, true);
        }

        public void Clear()
        {
            if (File.Exists(_filePath) == true)
            {
                File.Delete(_filePath);
            }
        }
    }
}
