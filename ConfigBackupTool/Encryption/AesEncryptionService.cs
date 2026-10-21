using System;
using System.IO;
using System.Security.Cryptography;

namespace ConfigBackupTool.Encryption
{
    public class AesEncryptionService : IEncryptionService
    {
        private const int KeySizeBytes = 32;
        private const int SaltSizeBytes = 16;
        private const int IvSizeBytes = 16;
        private const int Pbkdf2Iterations = 100000;

        public EncryptionResult Encrypt(byte[] plainText, string password)
        {
            string originalHash = ComputeHash(plainText);

            byte[] salt = new byte[SaltSizeBytes];
            RandomNumberGenerator.Fill(salt);

            byte[] iv = new byte[IvSizeBytes];
            RandomNumberGenerator.Fill(iv);

            byte[] key = DeriveKey(password, salt);
            byte[] cipherText;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySizeBytes * 8;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainText, 0, plainText.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    cipherText = memoryStream.ToArray();
                }
            }

            byte[] decryptedBack = Decrypt(cipherText, salt, iv, password);
            string decryptedHash = ComputeHash(decryptedBack);

            if (decryptedHash != originalHash)
            {
                throw new InvalidOperationException("Проверка целостности после шифрования не пройдена: хэш расшифрованных данных не совпадает с оригиналом.");
            }

            EncryptionResult result = new EncryptionResult();
            result.CipherText = cipherText;
            result.Salt = salt;
            result.IV = iv;
            result.OriginalHash = originalHash;

            return result;
        }

        public byte[] Decrypt(byte[] cipherText, byte[] salt, byte[] iv, string password)
        {
            byte[] key = DeriveKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySizeBytes * 8;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherText, 0, cipherText.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        public string ComputeHash(byte[] data)
        {
            byte[] hashBytes = SHA256.HashData(data);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(KeySizeBytes);
            }
        }
    }
}
