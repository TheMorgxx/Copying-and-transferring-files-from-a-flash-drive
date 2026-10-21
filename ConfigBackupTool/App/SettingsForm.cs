using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Games;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.App
{
    // Диалог настроек: редактирование known_roots.json (через IKnownRootsRepository) и
    // games.json (через IGameDefinitionRepository) в одном окне, две вкладки. Собрана в
    // коде, без отдельного Designer.cs (тот же подход, что и во всех остальных формах).
    //
    // Модель работы: обе коллекции загружаются в рабочие копии (_knownRoots/_games) при
    // открытии формы, все добавления/правки/удаления происходят только в памяти — на диск
    // ничего не пишется, пока пользователь явно не нажмёт "Сохранить". Есть отдельный флаг
    // _isDirty: если он взведён и форма закрывается (крестиком или кнопкой "Закрыть"),
    // FormClosing спрашивает, сохранить ли изменения — тот же принцип "не терять данные
    // молча", что и в остальных формах проекта.
    public class SettingsForm : Form
    {
        private readonly IKnownRootsRepository _knownRootsRepository;
        private readonly IGameDefinitionRepository _gameDefinitionRepository;

        private List<KnownRoot> _knownRoots;
        private List<GameDefinition> _games;
        private bool _isDirty;

        private TabControl _tabControl;

        private ListBox _knownRootsListBox;
        private Button _addKnownRootButton;
        private Button _editKnownRootButton;
        private Button _removeKnownRootButton;

        private ListBox _gamesListBox;
        private Button _addGameButton;
        private Button _renameGameButton;
        private Button _removeGameButton;

        private ListBox _pathsListBox;
        private Button _addPathButton;
        private Button _editPathButton;
        private Button _removePathButton;

        private Button _saveButton;
        private Button _closeButton;

        public SettingsForm(IKnownRootsRepository knownRootsRepository, IGameDefinitionRepository gameDefinitionRepository)
        {
            if (knownRootsRepository == null)
            {
                throw new ArgumentNullException("knownRootsRepository");
            }

            if (gameDefinitionRepository == null)
            {
                throw new ArgumentNullException("gameDefinitionRepository");
            }

            _knownRootsRepository = knownRootsRepository;
            _gameDefinitionRepository = gameDefinitionRepository;

            InitializeLayout();
            Load += OnFormLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeLayout()
        {
            Text = "Настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 470);

            _tabControl = new TabControl();
            _tabControl.Location = new Point(12, 12);
            _tabControl.Size = new Size(556, 400);

            TabPage knownRootsTab = BuildKnownRootsTab();
            TabPage gamesTab = BuildGamesTab();

            _tabControl.TabPages.Add(knownRootsTab);
            _tabControl.TabPages.Add(gamesTab);

            _saveButton = new Button();
            _saveButton.Text = "Сохранить";
            _saveButton.Location = new Point(356, 424);
            _saveButton.Size = new Size(100, 30);
            _saveButton.Click += OnSaveClick;

            _closeButton = new Button();
            _closeButton.Text = "Закрыть";
            _closeButton.Location = new Point(466, 424);
            _closeButton.Size = new Size(100, 30);
            _closeButton.Click += OnCloseClick;

            Controls.Add(_tabControl);
            Controls.Add(_saveButton);
            Controls.Add(_closeButton);
        }

        private TabPage BuildKnownRootsTab()
        {
            TabPage tab = new TabPage("Известные корни");

            _knownRootsListBox = new ListBox();
            _knownRootsListBox.Location = new Point(12, 12);
            _knownRootsListBox.Size = new Size(410, 340);
            _knownRootsListBox.SelectedIndexChanged += OnKnownRootsSelectedIndexChanged;

            _addKnownRootButton = new Button();
            _addKnownRootButton.Text = "Добавить...";
            _addKnownRootButton.Location = new Point(434, 12);
            _addKnownRootButton.Size = new Size(100, 28);
            _addKnownRootButton.Click += OnAddKnownRootClick;

            _editKnownRootButton = new Button();
            _editKnownRootButton.Text = "Изменить...";
            _editKnownRootButton.Location = new Point(434, 48);
            _editKnownRootButton.Size = new Size(100, 28);
            _editKnownRootButton.Click += OnEditKnownRootClick;

            _removeKnownRootButton = new Button();
            _removeKnownRootButton.Text = "Удалить";
            _removeKnownRootButton.Location = new Point(434, 84);
            _removeKnownRootButton.Size = new Size(100, 28);
            _removeKnownRootButton.Click += OnRemoveKnownRootClick;

            tab.Controls.Add(_knownRootsListBox);
            tab.Controls.Add(_addKnownRootButton);
            tab.Controls.Add(_editKnownRootButton);
            tab.Controls.Add(_removeKnownRootButton);

            return tab;
        }

        private TabPage BuildGamesTab()
        {
            TabPage tab = new TabPage("Игры");

            Label gamesLabel = new Label();
            gamesLabel.Text = "Игры:";
            gamesLabel.Location = new Point(12, 8);
            gamesLabel.AutoSize = true;

            _gamesListBox = new ListBox();
            _gamesListBox.Location = new Point(12, 26);
            _gamesListBox.Size = new Size(150, 300);
            _gamesListBox.SelectedIndexChanged += OnGamesSelectedIndexChanged;

            _addGameButton = new Button();
            _addGameButton.Text = "Добавить игру...";
            _addGameButton.Location = new Point(168, 26);
            _addGameButton.Size = new Size(90, 28);
            _addGameButton.Click += OnAddGameClick;

            _renameGameButton = new Button();
            _renameGameButton.Text = "Переименовать...";
            _renameGameButton.Location = new Point(168, 60);
            _renameGameButton.Size = new Size(90, 28);
            _renameGameButton.Click += OnRenameGameClick;

            _removeGameButton = new Button();
            _removeGameButton.Text = "Удалить игру";
            _removeGameButton.Location = new Point(168, 94);
            _removeGameButton.Size = new Size(90, 28);
            _removeGameButton.Click += OnRemoveGameClick;

            Label pathsLabel = new Label();
            pathsLabel.Text = "Пути выбранной игры:";
            pathsLabel.Location = new Point(268, 8);
            pathsLabel.AutoSize = true;

            _pathsListBox = new ListBox();
            _pathsListBox.Location = new Point(268, 26);
            _pathsListBox.Size = new Size(180, 300);
            _pathsListBox.SelectedIndexChanged += OnPathsSelectedIndexChanged;
            _pathsListBox.HorizontalScrollbar = true;

            _addPathButton = new Button();
            _addPathButton.Text = "Добавить путь...";
            _addPathButton.Location = new Point(454, 26);
            _addPathButton.Size = new Size(90, 28);
            _addPathButton.Click += OnAddPathClick;

            _editPathButton = new Button();
            _editPathButton.Text = "Изменить путь...";
            _editPathButton.Location = new Point(454, 60);
            _editPathButton.Size = new Size(90, 28);
            _editPathButton.Click += OnEditPathClick;

            _removePathButton = new Button();
            _removePathButton.Text = "Удалить путь";
            _removePathButton.Location = new Point(454, 94);
            _removePathButton.Size = new Size(90, 28);
            _removePathButton.Click += OnRemovePathClick;

            tab.Controls.Add(gamesLabel);
            tab.Controls.Add(_gamesListBox);
            tab.Controls.Add(_addGameButton);
            tab.Controls.Add(_renameGameButton);
            tab.Controls.Add(_removeGameButton);
            tab.Controls.Add(pathsLabel);
            tab.Controls.Add(_pathsListBox);
            tab.Controls.Add(_addPathButton);
            tab.Controls.Add(_editPathButton);
            tab.Controls.Add(_removePathButton);

            return tab;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                _knownRoots = new List<KnownRoot>(_knownRootsRepository.GetAll());
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _knownRoots = new List<KnownRoot>();
            }

            try
            {
                _games = _gameDefinitionRepository.GetAll();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _games = new List<GameDefinition>();
            }

            _isDirty = false;

            RefreshKnownRootsListBox(-1);
            RefreshGamesListBox(-1);
            RefreshPathsListBox();
            UpdateButtonsEnabled();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty == false)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(
                this,
                "Есть несохранённые изменения. Сохранить перед закрытием?",
                "ConfigBackupTool",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (answer == DialogResult.Yes)
            {
                bool saved = TrySave();
                if (saved == false)
                {
                    e.Cancel = true;
                }
            }
        }

        // --- Известные корни ---

        private void OnKnownRootsSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsEnabled();
        }

        private void OnAddKnownRootClick(object sender, EventArgs e)
        {
            using (KnownRootEditForm form = new KnownRootEditForm(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _knownRoots.Add(form.Result);
                    _isDirty = true;
                    RefreshKnownRootsListBox(_knownRoots.Count - 1);
                }
            }
        }

        private void OnEditKnownRootClick(object sender, EventArgs e)
        {
            int index = _knownRootsListBox.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            KnownRoot existing = _knownRoots[index];
            KnownRoot copy = new KnownRoot();
            copy.Marker = existing.Marker;
            copy.RootPath = existing.RootPath;

            using (KnownRootEditForm form = new KnownRootEditForm(copy))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _knownRoots[index] = form.Result;
                    _isDirty = true;
                    RefreshKnownRootsListBox(index);
                }
            }
        }

        private void OnRemoveKnownRootClick(object sender, EventArgs e)
        {
            int index = _knownRootsListBox.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            KnownRoot toRemove = _knownRoots[index];

            DialogResult answer = MessageBox.Show(
                this,
                "Удалить известный корень \"" + toRemove.Marker + " => " + toRemove.RootPath + "\"?",
                "ConfigBackupTool",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            _knownRoots.RemoveAt(index);
            _isDirty = true;
            RefreshKnownRootsListBox(-1);
        }

        private void RefreshKnownRootsListBox(int selectIndex)
        {
            _knownRootsListBox.Items.Clear();

            for (int i = 0; i < _knownRoots.Count; i++)
            {
                _knownRootsListBox.Items.Add(_knownRoots[i].Marker + "  =>  " + _knownRoots[i].RootPath);
            }

            if (selectIndex >= 0 && selectIndex < _knownRootsListBox.Items.Count)
            {
                _knownRootsListBox.SelectedIndex = selectIndex;
            }

            UpdateButtonsEnabled();
        }

        // --- Игры ---

        private void OnGamesSelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshPathsListBox();
            UpdateButtonsEnabled();
        }

        private void OnAddGameClick(object sender, EventArgs e)
        {
            using (TextInputForm form = new TextInputForm("Добавить игру", "Название игры:", null))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (IsGameNameTaken(form.InputText, -1) == true)
                {
                    MessageBox.Show(this, "Игра с таким названием уже есть в списке.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GameDefinition game = new GameDefinition();
                game.GameName = form.InputText;
                game.Paths = new List<PathEntry>();

                _games.Add(game);
                _isDirty = true;
                RefreshGamesListBox(_games.Count - 1);

                // ВРЕМЕННО (диагностика бага с пустым games.json): идентификатор объекта
                // _games сразу после Add, чтобы сравнить с тем, что увидим в TrySave().
                int hashAfterAdd = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_games);
                System.Diagnostics.Debug.WriteLine("OnAddGameClick: hash(_games) = " + hashAfterAdd + ", Count = " + _games.Count);
                MessageBox.Show(
                    this,
                    "[DEBUG] OnAddGameClick: hash(_games) = " + hashAfterAdd + ", Count = " + _games.Count,
                    "ConfigBackupTool DEBUG",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void OnRenameGameClick(object sender, EventArgs e)
        {
            int index = _gamesListBox.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            using (TextInputForm form = new TextInputForm("Переименовать игру", "Название игры:", _games[index].GameName))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (IsGameNameTaken(form.InputText, index) == true)
                {
                    MessageBox.Show(this, "Игра с таким названием уже есть в списке.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _games[index].GameName = form.InputText;
                _isDirty = true;
                RefreshGamesListBox(index);
            }
        }

        private void OnRemoveGameClick(object sender, EventArgs e)
        {
            int index = _gamesListBox.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(
                this,
                "Удалить игру \"" + _games[index].GameName + "\" вместе со всеми её путями?",
                "ConfigBackupTool",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            _games.RemoveAt(index);
            _isDirty = true;
            RefreshGamesListBox(-1);
        }

        private bool IsGameNameTaken(string name, int ignoreIndex)
        {
            for (int i = 0; i < _games.Count; i++)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                if (string.Equals(_games[i].GameName, name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshGamesListBox(int selectIndex)
        {
            _gamesListBox.Items.Clear();

            for (int i = 0; i < _games.Count; i++)
            {
                _gamesListBox.Items.Add(_games[i].GameName);
            }

            if (selectIndex >= 0 && selectIndex < _gamesListBox.Items.Count)
            {
                _gamesListBox.SelectedIndex = selectIndex;
            }

            RefreshPathsListBox();
            UpdateButtonsEnabled();
        }

        // --- Пути выбранной игры ---

        private void OnPathsSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsEnabled();
        }

        private void OnAddPathClick(object sender, EventArgs e)
        {
            int gameIndex = _gamesListBox.SelectedIndex;
            if (gameIndex < 0)
            {
                return;
            }

            using (PathEntryEditForm form = new PathEntryEditForm(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    PathEntry entry = new PathEntry();
                    entry.Path = form.ResultPath;

                    _games[gameIndex].Paths.Add(entry);
                    _isDirty = true;
                    RefreshPathsListBox();
                    _pathsListBox.SelectedIndex = _pathsListBox.Items.Count - 1;
                }
            }
        }

        private void OnEditPathClick(object sender, EventArgs e)
        {
            int gameIndex = _gamesListBox.SelectedIndex;
            int pathIndex = _pathsListBox.SelectedIndex;
            if (gameIndex < 0 || pathIndex < 0)
            {
                return;
            }

            string existingPath = _games[gameIndex].Paths[pathIndex].Path;

            using (PathEntryEditForm form = new PathEntryEditForm(existingPath))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _games[gameIndex].Paths[pathIndex].Path = form.ResultPath;
                    _isDirty = true;
                    RefreshPathsListBox();
                    _pathsListBox.SelectedIndex = pathIndex;
                }
            }
        }

        private void OnRemovePathClick(object sender, EventArgs e)
        {
            int gameIndex = _gamesListBox.SelectedIndex;
            int pathIndex = _pathsListBox.SelectedIndex;
            if (gameIndex < 0 || pathIndex < 0)
            {
                return;
            }

            string path = _games[gameIndex].Paths[pathIndex].Path;

            DialogResult answer = MessageBox.Show(
                this,
                "Удалить путь \"" + path + "\"?",
                "ConfigBackupTool",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            _games[gameIndex].Paths.RemoveAt(pathIndex);
            _isDirty = true;
            RefreshPathsListBox();
        }

        private void RefreshPathsListBox()
        {
            _pathsListBox.Items.Clear();

            int gameIndex = _gamesListBox.SelectedIndex;
            if (gameIndex < 0)
            {
                UpdateButtonsEnabled();
                return;
            }

            List<PathEntry> paths = _games[gameIndex].Paths;

            for (int i = 0; i < paths.Count; i++)
            {
                _pathsListBox.Items.Add(paths[i].Path);
            }

            UpdateButtonsEnabled();
        }

        // --- Общее ---

        private void UpdateButtonsEnabled()
        {
            bool knownRootSelected = _knownRootsListBox.SelectedIndex >= 0;
            _editKnownRootButton.Enabled = knownRootSelected;
            _removeKnownRootButton.Enabled = knownRootSelected;

            bool gameSelected = _gamesListBox.SelectedIndex >= 0;
            _renameGameButton.Enabled = gameSelected;
            _removeGameButton.Enabled = gameSelected;
            _addPathButton.Enabled = gameSelected;

            bool pathSelected = gameSelected == true && _pathsListBox.SelectedIndex >= 0;
            _editPathButton.Enabled = pathSelected;
            _removePathButton.Enabled = pathSelected;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            TrySave();
        }

        private bool TrySave()
        {
            // ВРЕМЕННО (диагностика бага с пустым games.json): идентификатор объекта
            // _games в точке входа TrySave, до вызова SaveAll — сравнить с хэшем,
            // напечатанным в OnAddGameClick.
            int hashAtSave = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_games);
            System.Diagnostics.Debug.WriteLine("TrySave: hash(_games) = " + hashAtSave + ", Count = " + _games.Count);
            MessageBox.Show(
                this,
                "[DEBUG] TrySave (до SaveAll): hash(_games) = " + hashAtSave + ", Count = " + _games.Count,
                "ConfigBackupTool DEBUG",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            try
            {
                _knownRootsRepository.SaveAll(_knownRoots);
                _gameDefinitionRepository.SaveAll(_games);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить настройки: " + ex.Message, "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            _isDirty = false;

            // ВРЕМЕННО (диагностика бага с пустым games.json): показываем реальное
            // количество игр в _games на момент вызова SaveAll. Убрать после того, как
            // баг локализован — заменить обратно на "Настройки сохранены.".
            MessageBox.Show(
                this,
                "Настройки сохранены. Игр в памяти на момент сохранения: " + _games.Count + ".",
                "ConfigBackupTool",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return true;
        }

        private void OnCloseClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
