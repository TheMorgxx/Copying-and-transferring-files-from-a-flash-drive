namespace ConfigBackupTool.Encryption
{
    public interface IEncryptionService
    {
        EncryptionResult Encrypt(byte[] plainText, string password);

        byte[] Decrypt(byte[] cipherText, byte[] salt, byte[] iv, string password);

        string ComputeHash(byte[] data);
    }
}
