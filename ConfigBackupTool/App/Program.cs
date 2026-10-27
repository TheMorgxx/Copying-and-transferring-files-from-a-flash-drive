using System;
using System.Windows.Forms;
using ConfigBackupTool.Credentials;
using ConfigBackupTool.Encryption;
using ConfigBackupTool.Games;
using ConfigBackupTool.Manifest;
using ConfigBackupTool.Managers;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Composition root: ручная сборка зависимостей, без DI-контейнера (см. п.5
            // архитектурных решений в README_ARCHITECTURE.md).
            //
            // IRestoreManager здесь намеренно НЕ собирается — ему нужен IRestoreConflictResolver,
            // а его WinForms-реализация (ConflictForm) появится на шаге 5. Вместо этого в MainForm
            // прокидываются составные части (pathResolver/encryptionService/manifestRepository),
            // из которых ImportForm на шаге 5 сама соберёт RestoreManager с готовым резолвером.
            IKnownRootsRepository knownRootsRepository = new JsonKnownRootsRepository();
            IGameDefinitionRepository gameDefinitionRepository = new JsonGameDefinitionRepository();

            IPathNormalizer pathNormalizer = new PathNormalizer();
            IPathResolver pathResolver = new PathResolver(knownRootsRepository);

            IEncryptionService encryptionService = new AesEncryptionService();
            IBackupManifestRepository manifestRepository = new JsonManifestRepository(encryptionService);

            IBackupManager backupManager = new BackupManager(pathNormalizer, encryptionService, manifestRepository);

            ICredentialStore credentialStore = new DpapiCredentialStore();

            MainForm mainForm = new MainForm(
                gameDefinitionRepository,
                knownRootsRepository,
                backupManager,
                pathResolver,
                encryptionService,
                manifestRepository,
                credentialStore);

            Application.Run(mainForm);
        }
    }
}
