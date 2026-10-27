using System;
using System.Drawing;
using System.Windows.Forms;
using ConfigBackupTool.Credentials;
using ConfigBackupTool.Encryption;
using ConfigBackupTool.Games;
using ConfigBackupTool.Manifest;
using ConfigBackupTool.Managers;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.App
{
    // Тонкий хаб: 3 кнопки, без собственной бизнес-логики (см. п.6 архитектурных решений
    // в README_ARCHITECTURE.md). Форма собрана в коде, без отдельного Designer.cs — для
    // такой простой раскладки это короче и не требует лишнего файла.
    public class MainForm : Form
    {
        private readonly IGameDefinitionRepository _gameDefinitionRepository;
        private readonly IKnownRootsRepository _knownRootsRepository;
        private readonly IBackupManager _backupManager;
        private readonly IPathResolver _pathResolver;
        private readonly IEncryptionService _encryptionService;
        private readonly IBackupManifestRepository _manifestRepository;
        private readonly ICredentialStore _credentialStore;

        public MainForm(
            IGameDefinitionRepository gameDefinitionRepository,
            IKnownRootsRepository knownRootsRepository,
            IBackupManager backupManager,
            IPathResolver pathResolver,
            IEncryptionService encryptionService,
            IBackupManifestRepository manifestRepository,
            ICredentialStore credentialStore)
        {
            if (gameDefinitionRepository == null)
            {
                throw new ArgumentNullException("gameDefinitionRepository");
            }

            if (knownRootsRepository == null)
            {
                throw new ArgumentNullException("knownRootsRepository");
            }

            if (backupManager == null)
            {
                throw new ArgumentNullException("backupManager");
            }

            if (pathResolver == null)
            {
                throw new ArgumentNullException("pathResolver");
            }

            if (encryptionService == null)
            {
                throw new ArgumentNullException("encryptionService");
            }

            if (manifestRepository == null)
            {
                throw new ArgumentNullException("manifestRepository");
            }

            if (credentialStore == null)
            {
                throw new ArgumentNullException("credentialStore");
            }

            _gameDefinitionRepository = gameDefinitionRepository;
            _knownRootsRepository = knownRootsRepository;
            _backupManager = backupManager;
            _pathResolver = pathResolver;
            _encryptionService = encryptionService;
            _manifestRepository = manifestRepository;
            _credentialStore = credentialStore;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Text = "ConfigBackupTool";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(280, 220);

            Button exportButton = CreateHubButton("Экспорт (бэкап)", 30);
            exportButton.Click += OnExportClick;

            Button importButton = CreateHubButton("Восстановление", 90);
            importButton.Click += OnImportClick;

            Button settingsButton = CreateHubButton("Настройки", 150);
            settingsButton.Click += OnSettingsClick;

            Controls.Add(exportButton);
            Controls.Add(importButton);
            Controls.Add(settingsButton);
        }

        private Button CreateHubButton(string text, int top)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(240, 40);
            button.Location = new Point(20, top);
            return button;
        }

        // ExportForm подключена (шаг 4).
        private void OnExportClick(object sender, EventArgs e)
        {
            using (ExportForm exportForm = new ExportForm(_gameDefinitionRepository, _backupManager, _credentialStore))
            {
                exportForm.ShowDialog(this);
            }
        }

        // ImportForm подключена (шаг 5). Она сама собирает RestoreManager из
        // _pathResolver/_encryptionService/_manifestRepository + WinFormsRestoreConflictResolver.
        private void OnImportClick(object sender, EventArgs e)
        {
            using (ImportForm importForm = new ImportForm(_pathResolver, _encryptionService, _manifestRepository, _credentialStore))
            {
                importForm.ShowDialog(this);
            }
        }

        // SettingsForm подключена (шаг 6): редактирование known_roots.json и games.json
        // через _knownRootsRepository и _gameDefinitionRepository.
        private void OnSettingsClick(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm(_knownRootsRepository, _gameDefinitionRepository))
            {
                settingsForm.ShowDialog(this);
            }
        }
    }
}
