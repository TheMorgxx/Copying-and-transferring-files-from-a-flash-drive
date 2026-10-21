using System;
using System.Drawing;
using System.Windows.Forms;

namespace ConfigBackupTool.App
{
    // Модальный диалог добавления/редактирования одного пути в списке игры (PathEntry.Path).
    // Путь может быть как файлом, так и папкой (см. games.json.example), поэтому есть два
    // отдельных "Обзор..." — под файл и под папку — плюс сама строка редактируется вручную:
    // это единственный способ задать путь, которого сейчас не существует на этой машине
    // (games.json может редактироваться не на той машине, где путь реально есть).
    // Слэши не нормализуются здесь намеренно — PathNormalizer.Normalize сам меняет "\" на "/"
    // при экспорте, дублировать эту логику в UI незачем.
    public class PathEntryEditForm : Form
    {
        private TextBox _pathTextBox;
        private Button _browseFileButton;
        private Button _browseFolderButton;
        private Button _okButton;
        private Button _cancelButton;

        public string ResultPath { get; private set; }

        public PathEntryEditForm(string existingPath)
        {
            InitializeLayout();

            if (existingPath != null)
            {
                _pathTextBox.Text = existingPath;
            }
        }

        private void InitializeLayout()
        {
            Text = "Путь для бэкапа";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 150);

            Label pathLabel = new Label();
            pathLabel.Text = "Путь (файл или папка), можно ввести вручную:";
            pathLabel.Location = new Point(15, 12);
            pathLabel.AutoSize = true;

            _pathTextBox = new TextBox();
            _pathTextBox.Location = new Point(15, 34);
            _pathTextBox.Size = new Size(430, 23);

            _browseFileButton = new Button();
            _browseFileButton.Text = "Обзор файл...";
            _browseFileButton.Location = new Point(15, 68);
            _browseFileButton.Size = new Size(140, 25);
            _browseFileButton.Click += OnBrowseFileClick;

            _browseFolderButton = new Button();
            _browseFolderButton.Text = "Обзор папку...";
            _browseFolderButton.Location = new Point(163, 68);
            _browseFolderButton.Size = new Size(140, 25);
            _browseFolderButton.Click += OnBrowseFolderClick;

            _okButton = new Button();
            _okButton.Text = "ОК";
            _okButton.Location = new Point(276, 110);
            _okButton.Size = new Size(80, 28);
            _okButton.Click += OnOkClick;

            _cancelButton = new Button();
            _cancelButton.Text = "Отмена";
            _cancelButton.Location = new Point(362, 110);
            _cancelButton.Size = new Size(83, 28);
            _cancelButton.Click += OnCancelClick;

            Controls.Add(pathLabel);
            Controls.Add(_pathTextBox);
            Controls.Add(_browseFileButton);
            Controls.Add(_browseFolderButton);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void OnBrowseFileClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите файл";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _pathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void OnBrowseFolderClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _pathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pathTextBox.Text) == true)
            {
                MessageBox.Show(this, "Введите или выберите путь.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ResultPath = _pathTextBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            ResultPath = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
