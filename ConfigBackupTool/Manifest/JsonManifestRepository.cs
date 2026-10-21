using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Encryption;

namespace ConfigBackupTool.Manifest
{
    public class JsonManifestRepository : IBackupManifestRepository
    {
        private readonly IEncryptionService _encryptionService;

        public JsonManifestRepository(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public void Save(BackupManifest manifest, string filePath, string password)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string manifestJson = JsonSerializer.Serialize(manifest, options);
            byte[] plainBytes = Encoding.UTF8.GetBytes(manifestJson);

            EncryptionResult encryptionResult = _encryptionService.Encrypt(plainBytes, password);

            EncryptedManifestFile encryptedFile = new EncryptedManifestFile();
            encryptedFile.Salt = Convert.ToBase64String(encryptionResult.Salt);
            encryptedFile.IV = Convert.ToBase64String(encryptionResult.IV);
            encryptedFile.CipherText = Convert.ToBase64String(encryptionResult.CipherText);

            string wrapperJson = JsonSerializer.Serialize(encryptedFile, options);
            File.WriteAllText(filePath, wrapperJson, Encoding.UTF8);
        }

        public BackupManifest Load(string filePath, string password)
        {
            string wrapperJson = File.ReadAllText(filePath, Encoding.UTF8);
            EncryptedManifestFile encryptedFile;

            try
            {
                encryptedFile = JsonSerializer.Deserialize<EncryptedManifestFile>(wrapperJson);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("manifest.json повреждён или имеет неверный формат.");
            }

            if (encryptedFile == null)
            {
                throw new InvalidOperationException("Не удалось прочитать manifest.json: файл повреждён или имеет неверный формат.");
            }

            byte[] salt;
            byte[] iv;
            byte[] cipherText;

            try
            {
                salt = Convert.FromBase64String(encryptedFile.Salt);
                iv = Convert.FromBase64String(encryptedFile.IV);
                cipherText = Convert.FromBase64String(encryptedFile.CipherText);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("manifest.json повреждён или имеет неверный формат.");
            }
            catch (ArgumentNullException)
            {
                // Валидный JSON, но без обязательных полей (например "{}")
                throw new InvalidOperationException("manifest.json повреждён или имеет неверный формат.");
            }

            byte[] plainBytes;

            try
            {
                plainBytes = _encryptionService.Decrypt(cipherText, salt, iv, password);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Не удалось расшифровать manifest.json: неверный пароль или повреждённые данные.");
            }

            string manifestJson = Encoding.UTF8.GetString(plainBytes);
            BackupManifest manifest;

            try
            {
                manifest = JsonSerializer.Deserialize<BackupManifest>(manifestJson);
            }
            catch (JsonException)
            {
                // Неверный пароль с валидным PKCS7-паддингом (~1/256): расшифровка "удалась", но
                // получился мусор, не являющийся JSON манифеста.
                throw new InvalidOperationException("Не удалось расшифровать manifest.json: неверный пароль или повреждённые данные.");
            }

            if (manifest == null)
            {
                throw new InvalidOperationException("manifest.json расшифрован, но не удалось десериализовать содержимое.");
            }

            return manifest;
        }
    }
}
