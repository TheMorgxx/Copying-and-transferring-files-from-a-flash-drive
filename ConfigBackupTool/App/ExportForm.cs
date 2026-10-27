using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConfigBackupTool.Credentials;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Games;
using ConfigBackupTool.Managers;

namespace ConfigBackupTool.App
{
    // Диалог экспорта: чекбоксы игр из games.json, базовая папка (внутри создаётся
    // Backup_yyyyMMdd_HHmmss/), пароль дважды с подтверждением, живой построчный лог.
    // Собрана в коде, без отдельного Designer.cs (см. MainForm.cs — тот же подход).
    public class ExportForm : Form
    {
        private readonly IGameDefinitionRepository _gameDefinitionRepository;
        private readonly IBackupManager _backupManager;
        private readonly ICredentialStore _credentialStore;

        private List<GameDefinition> _allGames;
        private string _baseFolder;
        private bool _isRunning;

        private CheckedListBox _gamesCheckedListBox;
        private TextBox _destinationFolderTextBox;
        private Button _browseFolderButton;
        private TextBox _passwordTextBox;
        private TextBox _passwordConfirmTextBox;
        private CheckBox _rememberPasswordCheckBox;
        private TextBox _logTextBox;
        private Button _startButton;

        public ExportForm(IGameDefinitionRepository gameDefinitionRepository, IBackupManager backupManager, ICredentialStore credentialStore)
        {
            if (gameDefinitionRepository == null)
            {
                throw new ArgumentNullException("gameDefinitionRepository");
            }

            if (backupManager == null)
            {
                throw new ArgumentNullException("backupManager");
            }

            if (credentialStore == null)
            {
                throw new ArgumentNullException("credentialStore");
            }

            _gameDefinitionRepository = gameDefinitionRepository;
            _backupManager = backupManager;
            _credentialStore = credentialStore;
            _allGames = new List<GameDefinition>();

            InitializeLayout();
            Load += OnFormLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeLayout()
        {
            Text = "Экспорт (бэкап)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 480);

            Label gamesLabel = new Label();
            gamesLabel.Text = "Игры для бэкапа:";
            gamesLabel.Location = new Point(15, 12);
            gamesLabel.AutoSize = true;

            _gamesCheckedListBox = new CheckedListBox();
            _gamesCheckedListBox.Location = new Point(15, 32);
            _gamesCheckedListBox.Size = new Size(450, 140);
            _gamesCheckedListBox.CheckOnClick = true;

            Label folderLabel = new Label();
            folderLabel.Text = "Базовая папка (флешка):";
            folderLabel.Location = new Point(15, 182);
            folderLabel.AutoSize = true;

            _destinationFolderTextBox = new TextBox();
            _destinationFolderTextBox.Location = new Point(15, 202);
            _destinationFolderTextBox.Size = new Size(345, 23);
            _destinationFolderTextBox.ReadOnly = true;

            _browseFolderButton = new Button();
            _browseFolderButton.Text = "Обзор...";
            _browseFolderButton.Location = new Point(368, 201);
            _browseFolderButton.Size = new Size(97, 25);
            _browseFolderButton.Click += OnBrowseFolderClick;

            Label passwordLabel = new Label();
            passwordLabel.Text = "Пароль:";
            passwordLabel.Location = new Point(15, 240);
            passwordLabel.AutoSize = true;

            _passwordTextBox = new TextBox();
            _passwordTextBox.Location = new Point(15, 260);
            _passwordTextBox.Size = new Size(450, 23);
            _passwordTextBox.UseSystemPasswordChar = true;

            Label passwordConfirmLabel = new Label();
            passwordConfirmLabel.Text = "Повторите пароль:";
            passwordConfirmLabel.Location = new Point(15, 292);
            passwordConfirmLabel.AutoSize = true;

            _passwordConfirmTextBox = new TextBox();
            _passwordConfirmTextBox.Location = new Point(15, 312);
            _passwordConfirmTextBox.Size = new Size(450, 23);
            _passwordConfirmTextBox.UseSystemPasswordChar = true;

            Label logLabel = new Label();
            logLabel.Text = "Лог:";
            logLabel.Location = new Point(15, 344);
            logLabel.AutoSize = true;

            _logTextBox = new TextBox();
            _logTextBox.Location = new Point(15, 364);
            _logTextBox.Size = new Size(450, 70);
            _logTextBox.Multiline = true;
            _logTextBox.ReadOnly = true;
            _logTextBox.ScrollBars = ScrollBars.Vertical;

            _startButton = new Button();
            _startButton.Text = "Начать экспорт";
            _startButton.Location = new Point(300, 442);
            _startButton.Size = new Size(165, 28);
            _startButton.Click += OnStartClick;

            _rememberPasswordCheckBox = new CheckBox();
            _rememberPasswordCheckBox.Text = "Запомнить пароль на этом ПК";
            _rememberPasswordCheckBox.Location = new Point(15, 447);
            _rememberPasswordCheckBox.AutoSize = true;

            Controls.Add(gamesLabel);
            Controls.Add(_gamesCheckedListBox);
            Controls.Add(folderLabel);
            Controls.Add(_destinationFolderTextBox);
            Controls.Add(_browseFolderButton);
            Controls.Add(passwordLabel);
            Controls.Add(_passwordTextBox);
            Controls.Add(passwordConfirmLabel);
            Controls.Add(_passwordConfirmTextBox);
            Controls.Add(logLabel);
            Controls.Add(_logTextBox);
            Controls.Add(_rememberPasswordCheckBox);
            Controls.Add(_startButton);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                _allGames = _gameDefinitionRepository.GetAll();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allGames = new List<GameDefinition>();
            }

            _gamesCheckedListBox.Items.Clear();

            for (int i = 0; i < _allGames.Count; i++)
            {
                _gamesCheckedListBox.Items.Add(_allGames[i].GameName);
            }

            if (_allGames.Count == 0)
            {
                AppendLog("games.json пуст или не найден — нечего экспортировать. Заполните список игр (SettingsForm, шаг 6) и откройте форму заново.");
                _startButton.Enabled = false;
            }

            string rememberedPassword = _credentialStore.Load();

            if (rememberedPassword != null)
            {
                _passwordTextBox.Text = rememberedPassword;
                _passwordConfirmTextBox.Text = rememberedPassword;
                _rememberPasswordCheckBox.Checked = true;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRunning == true)
            {
                MessageBox.Show(this, "Экспорт ещё выполняется, дождитесь завершения.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void OnBrowseFolderClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите базовую папку (например, флешку) — внутри неё будет создана подпапка Backup_ГГГГММДД_ЧЧММСС";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _baseFolder = dialog.SelectedPath;
                    _destinationFolderTextBox.Text = _baseFolder;
                }
            }
        }

        private async void OnStartClick(object sender, EventArgs e)
        {
            List<GameDefinition> selectedGames = CollectSelectedGames();

            if (selectedGames.Count == 0)
            {
                MessageBox.Show(this, "Отметьте хотя бы одну игру.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_baseFolder) == true)
            {
                MessageBox.Show(this, "Выберите базовую папку.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string password = _passwordTextBox.Text;
            string passwordConfirm = _passwordConfirmTextBox.Text;

            if (string.IsNullOrEmpty(password) == true)
            {
                MessageBox.Show(this, "Введите пароль.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password != passwordConfirm)
            {
                MessageBox.Show(this, "Пароли не совпадают.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string destinationFolder = Path.Combine(_baseFolder, "Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            try
            {
                Directory.CreateDirectory(destinationFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось создать папку бэкапа: " + ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetRunningState(true);
            _logTextBox.Clear();
            AppendLog("Экспорт начат: " + destinationFolder);

            IProgress<string> progress = new Progress<string>(AppendLog);

            try
            {
                List<string> log = await Task.Run(() => _backupManager.Export(selectedGames, destinationFolder, password, progress));
                AppendLog("Готово. Строк в логе: " + log.Count);
                ApplyRememberChoice(password);
                MessageBox.Show(this, "Экспорт завершён.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка экспорта: " + ex.Message);
                MessageBox.Show(this, "Экспорт завершился с ошибкой: " + ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetRunningState(false);
            }
        }

        // Вызывается только после успешного экспорта: сохранённый пароль всегда совпадает с тем,
        // которым реально зашифрован последний бэкап. Сбой хранилища экспорт не валит — только лог.
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

        private List<GameDefinition> CollectSelectedGames()
        {
            List<GameDefinition> selected = new List<GameDefinition>();

            for (int i = 0; i < _gamesCheckedListBox.Items.Count; i++)
            {
                if (_gamesCheckedListBox.GetItemChecked(i) == true)
                {
                    selected.Add(_allGames[i]);
                }
            }

            return selected;
        }

        private void AppendLog(string line)
        {
            _logTextBox.AppendText(line + Environment.NewLine);
        }

        private void SetRunningState(bool isRunning)
        {
            _isRunning = isRunning;

            _gamesCheckedListBox.Enabled = isRunning == false;
            _browseFolderButton.Enabled = isRunning == false;
            _passwordTextBox.Enabled = isRunning == false;
            _passwordConfirmTextBox.Enabled = isRunning == false;
            _rememberPasswordCheckBox.Enabled = isRunning == false;
            _startButton.Enabled = isRunning == false;
        }
    }
}
