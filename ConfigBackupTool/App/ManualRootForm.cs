using System;
using System.Drawing;
using System.Windows.Forms;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.App
{
    // Вызывается один раз на игру, только если ВСЕ DriveRelative-элементы этой игры дали
    // 0 кандидатов при обычном резолве (см. IRestoreManager.Import — manualRootResolver).
    // Обёртка вокруг FolderBrowserDialog с подсказкой по Tail референсного элемента, чтобы
    // пользователь понимал, какую именно папку от него просят выбрать.
    public class ManualRootForm : Form
    {
        private readonly BackupItem _referenceItem;

        private TextBox _selectedFolderTextBox;
        private Button _browseButton;
        private Button _okButton;
        private Button _cancelButton;

        public string SelectedFolder { get; private set; }

        public ManualRootForm(BackupItem referenceItem)
        {
            if (referenceItem == null)
            {
                throw new ArgumentNullException("referenceItem");
            }

            _referenceItem = referenceItem;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Text = "Не удалось найти папку игры автоматически";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 170);

            Label hintLabel = new Label();
            hintLabel.Text = "Игра: " + _referenceItem.GameName + "\r\nИщем папку, содержащую: " + _referenceItem.Tail + "\r\nВыберите корневую папку игры вручную.";
            hintLabel.Location = new Point(15, 12);
            hintLabel.Size = new Size(430, 60);

            _selectedFolderTextBox = new TextBox();
            _selectedFolderTextBox.Location = new Point(15, 80);
            _selectedFolderTextBox.Size = new Size(345, 23);
            _selectedFolderTextBox.ReadOnly = true;

            _browseButton = new Button();
            _browseButton.Text = "Обзор...";
            _browseButton.Location = new Point(368, 79);
            _browseButton.Size = new Size(77, 25);
            _browseButton.Click += OnBrowseClick;

            _okButton = new Button();
            _okButton.Text = "ОК";
            _okButton.Location = new Point(276, 128);
            _okButton.Size = new Size(80, 28);
            _okButton.Click += OnOkClick;

            _cancelButton = new Button();
            _cancelButton.Text = "Пропустить игру";
            _cancelButton.Location = new Point(362, 128);
            _cancelButton.Size = new Size(83, 28);
            _cancelButton.Click += OnCancelClick;

            Controls.Add(hintLabel);
            Controls.Add(_selectedFolderTextBox);
            Controls.Add(_browseButton);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите корневую папку игры \"" + _referenceItem.GameName + "\"";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderTextBox.Text) == true)
            {
                MessageBox.Show(this, "Выберите папку через \"Обзор...\" или нажмите \"Пропустить игру\".", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedFolder = _selectedFolderTextBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            SelectedFolder = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
