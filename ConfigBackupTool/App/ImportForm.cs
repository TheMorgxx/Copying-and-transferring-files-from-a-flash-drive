using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConfigBackupTool.Credentials;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Encryption;
using ConfigBackupTool.Manifest;
using ConfigBackupTool.Managers;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.App
{
    // Диалог восстановления: FolderBrowserDialog на папку СЕССИИ бэкапа (не на файл
    // manifest.json — он ожидается внутри неё), пароль один раз, переключатели RestoreMode,
    // живой лог. Собирает RestoreManager сама (см. Program.cs — там его собрать было нельзя,
    // не было WinForms-реализации IRestoreConflictResolver).
    public class ImportForm : Form
    {
        private readonly IPathResolver _pathResolver;
        private readonly IEncryptionService _encryptionService;
        private readonly IBackupManifestRepository _manifestRepository;
        private readonly IRestoreManager _restoreManager;
        private readonly ICredentialStore _credentialStore;

        private string _sessionFolder;
        private bool _isRunning;

        private TextBox _sessionFolderTextBox;
        private Button _browseFolderButton;
        private TextBox _passwordTextBox;
        private CheckBox _rememberPasswordCheckBox;
        private RadioButton _replaceAllRadio;
        private RadioButton _addMissingOnlyRadio;
        private RadioButton _askOnConflictRadio;
        private TextBox _logTextBox;
        private Button _startButton;

        public ImportForm(IPathResolver pathResolver, IEncryptionService encryptionService, IBackupManifestRepository manifestRepository, ICredentialStore credentialStore)
        {
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

            _pathResolver = pathResolver;
            _encryptionService = encryptionService;
            _manifestRepository = manifestRepository;
            _credentialStore = credentialStore;

            // WinFormsRestoreConflictResolver держит ссылку на эту форму (this), чтобы
            // маршалить показ ConflictForm на UI-поток из фонового Task.Run — см. её
            // комментарий и OnStartClick ниже.
            _restoreManager = new RestoreManager(_pathResolver, _encryptionService, _manifestRepository, new WinFormsRestoreConflictResolver(this));

            InitializeLayout();
            Load += OnFormLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeLayout()
        {
            Text = "Восстановление";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 420);

            Label folderLabel = new Label();
            folderLabel.Text = "Папка сессии бэкапа (внутри должен быть manifest.json):";
            folderLabel.Location = new Point(15, 12);
            folderLabel.AutoSize = true;

            _sessionFolderTextBox = new TextBox();
            _sessionFolderTextBox.Location = new Point(15, 32);
            _sessionFolderTextBox.Size = new Size(345, 23);
            _sessionFolderTextBox.ReadOnly = true;

            _browseFolderButton = new Button();
            _browseFolderButton.Text = "Обзор...";
            _browseFolderButton.Location = new Point(368, 31);
            _browseFolderButton.Size = new Size(97, 25);
            _browseFolderButton.Click += OnBrowseFolderClick;

            Label passwordLabel = new Label();
            passwordLabel.Text = "Пароль:";
            passwordLabel.Location = new Point(15, 68);
            passwordLabel.AutoSize = true;

            _passwordTextBox = new TextBox();
            _passwordTextBox.Location = new Point(15, 88);
            _passwordTextBox.Size = new Size(450, 23);
            _passwordTextBox.UseSystemPasswordChar = true;

            Label modeLabel = new Label();
            modeLabel.Text = "Режим восстановления:";
            modeLabel.Location = new Point(15, 122);
            modeLabel.AutoSize = true;

            _replaceAllRadio = new RadioButton();
            _replaceAllRadio.Text = "Заменить всё (ReplaceAll)";
            _replaceAllRadio.Location = new Point(15, 144);
            _replaceAllRadio.AutoSize = true;

            _addMissingOnlyRadio = new RadioButton();
            _addMissingOnlyRadio.Text = "Только добавить недостающее (AddMissingOnly)";
            _addMissingOnlyRadio.Location = new Point(15, 168);
            _addMissingOnlyRadio.AutoSize = true;

            _askOnConflictRadio = new RadioButton();
            _askOnConflictRadio.Text = "Спрашивать при конфликте (AskOnConflict)";
            _askOnConflictRadio.Location = new Point(15, 192);
            _askOnConflictRadio.AutoSize = true;
            _askOnConflictRadio.Checked = true;

            Label logLabel = new Label();
            logLabel.Text = "Лог:";
            logLabel.Location = new Point(15, 224);
            logLabel.AutoSize = true;

            _logTextBox = new TextBox();
            _logTextBox.Location = new Point(15, 244);
            _logTextBox.Size = new Size(450, 130);
            _logTextBox.Multiline = true;
            _logTextBox.ReadOnly = true;
            _logTextBox.ScrollBars = ScrollBars.Vertical;

            _startButton = new Button();
            _startButton.Text = "Начать восстановление";
            _startButton.Location = new Point(280, 382);
            _startButton.Size = new Size(185, 28);
            _startButton.Click += OnStartClick;

            _rememberPasswordCheckBox = new CheckBox();
            _rememberPasswordCheckBox.Text = "Запомнить пароль на этом ПК";
            _rememberPasswordCheckBox.Location = new Point(15, 387);
            _rememberPasswordCheckBox.AutoSize = true;

            Controls.Add(folderLabel);
            Controls.Add(_sessionFolderTextBox);
            Controls.Add(_browseFolderButton);
            Controls.Add(passwordLabel);
            Controls.Add(_passwordTextBox);
            Controls.Add(modeLabel);
            Controls.Add(_replaceAllRadio);
            Controls.Add(_addMissingOnlyRadio);
            Controls.Add(_askOnConflictRadio);
            Controls.Add(logLabel);
            Controls.Add(_logTextBox);
            Controls.Add(_rememberPasswordCheckBox);
            Controls.Add(_startButton);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            string rememberedPassword = _credentialStore.Load();

            if (rememberedPassword != null)
            {
                _passwordTextBox.Text = rememberedPassword;
                _rememberPasswordCheckBox.Checked = true;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRunning == true)
            {
                MessageBox.Show(this, "Восстановление ещё выполняется, дождитесь завершения.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void OnBrowseFolderClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку сессии бэкапа (ту, что была создана при экспорте, с manifest.json внутри)";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _sessionFolder = dialog.SelectedPath;
                    _sessionFolderTextBox.Text = _sessionFolder;
                }
            }
        }

        private async void OnStartClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_sessionFolder) == true)
            {
                MessageBox.Show(this, "Выберите папку сессии бэкапа.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string manifestPath = Path.Combine(_sessionFolder, "manifest.json");

            if (File.Exists(manifestPath) == false)
            {
                MessageBox.Show(this, "В выбранной папке нет manifest.json.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string password = _passwordTextBox.Text;

            if (string.IsNullOrEmpty(password) == true)
            {
                MessageBox.Show(this, "Введите пароль.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RestoreMode mode = GetSelectedMode();

            SetRunningState(true);
            _logTextBox.Clear();
            AppendLog("Восстановление начато: " + _sessionFolder);

            IProgress<string> progress = new Progress<string>(AppendLog);

            try
            {
                List<string> log = await Task.Run(() => _restoreManager.Import(
                    manifestPath,
                    _sessionFolder,
                    password,
                    mode,
                    ResolveAmbiguity,
                    ResolveManualRoot,
                    progress));

                AppendLog("Готово. Строк в логе: " + log.Count);
                ApplyRememberChoice(password);
                MessageBox.Show(this, "Восстановление завершено.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка восстановления: " + ex.Message);
                MessageBox.Show(this, "Восстановление завершилось с ошибкой: " + ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetRunningState(false);
            }
        }

        // Вызывается RestoreManager синхронно из фонового Task.Run — на UI-поток нужно
        // выйти вручную через Invoke, Progress<T> здесь не годится (нужен возврат значения).
        private string ResolveAmbiguity(IReadOnlyList<string> candidates)
        {
            Func<string> showDialog = delegate
            {
                using (AmbiguityForm form = new AmbiguityForm(candidates))
                {
                    form.ShowDialog(this);
                    return form.SelectedPath;
                }
            };

            if (InvokeRequired == true)
            {
                return (string)Invoke(showDialog);
            }

            return showDialog();
        }

        private string ResolveManualRoot(BackupItem referenceItem)
        {
            Func<string> showDialog = delegate
            {
                using (ManualRootForm form = new ManualRootForm(referenceItem))
                {
                    form.ShowDialog(this);
                    return form.SelectedFolder;
                }
            };

            if (InvokeRequired == true)
            {
                return (string)Invoke(showDialog);
            }

            return showDialog();
        }

        // Вызывается только после успешного восстановления — то есть пароль точно верный
        // (неверный пароль роняет Load манифеста раньше). Сбой хранилища результат не портит.
        private void ApplyRememberChoice(string password)
        {
            try
            {
                if (_rememberPasswordCheckBox.Checked == true)
                {
                    _credentialStore.Save(password);
                }
                else
                {
                    _credentialStore.Clear();
                }
            }
            catch (Exception ex)
            {
                AppendLog("Не удалось обновить сохранённый пароль: " + ex.Message);
            }
        }

        private RestoreMode GetSelectedMode()
        {
            if (_replaceAllRadio.Checked == true)
            {
                return RestoreMode.ReplaceAll;
            }

            if (_addMissingOnlyRadio.Checked == true)
            {
                return RestoreMode.AddMissingOnly;
            }

            return RestoreMode.AskOnConflict;
        }

        private void AppendLog(string line)
        {
            _logTextBox.AppendText(line + Environment.NewLine);
        }

        private void SetRunningState(bool isRunning)
        {
            _isRunning = isRunning;

            _browseFolderButton.Enabled = isRunning == false;
            _passwordTextBox.Enabled = isRunning == false;
            _rememberPasswordCheckBox.Enabled = isRunning == false;
            _replaceAllRadio.Enabled = isRunning == false;
            _addMissingOnlyRadio.Enabled = isRunning == false;
            _askOnConflictRadio.Enabled = isRunning == false;
            _startButton.Enabled = isRunning == false;
        }
    }
}
