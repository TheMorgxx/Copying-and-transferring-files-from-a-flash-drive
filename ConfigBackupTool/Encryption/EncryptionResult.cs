namespace ConfigBackupTool.Encryption
{
    public class EncryptionResult
    {
        public byte[] CipherText { get; set; }
        public byte[] Salt { get; set; }
        public byte[] IV { get; set; }
        public string OriginalHash { get; set; }
    }
}
