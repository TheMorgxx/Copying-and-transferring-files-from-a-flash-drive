using System;
using System.Drawing;
using System.Windows.Forms;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.App
{
    // Модальный диалог добавления/редактирования одного известного корня
    // (см. IKnownRootsRepository.KnownRoot). existingRoot == null — режим добавления,
    // иначе поля предзаполняются его значениями (вызывающий код передаёт копию, а не
    // ссылку на элемент рабочего списка SettingsForm — сама форма ничего не мутирует
    // до нажатия "ОК").
    public class KnownRootEditForm : Form
    {
        private TextBox _markerTextBox;
        private TextBox _rootPathTextBox;
        private Button _browseButton;
        private Button _okButton;
        private Button _cancelButton;

        public KnownRoot Result { get; private set; }

        public KnownRootEditForm(KnownRoot existingRoot)
        {
            InitializeLayout();

            if (existingRoot != null)
            {
                _markerTextBox.Text = existingRoot.Marker;
                _rootPathTextBox.Text = existingRoot.RootPath;
            }
        }

        private void InitializeLayout()
        {
            Text = "Известный корень";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 170);

            Label markerLabel = new Label();
            markerLabel.Text = "Маркер (должен совпадать с одним из известных PathNormalizer, например \"steamapps/common/\"):";
            markerLabel.Location = new Point(15, 12);
            markerLabel.Size = new Size(400, 32);

            _markerTextBox = new TextBox();
            _markerTextBox.Location = new Point(15, 48);
            _markerTextBox.Size = new Size(400, 23);

            Label rootPathLabel = new Label();
            rootPathLabel.Text = "Корневая папка на этой машине:";
            rootPathLabel.Location = new Point(15, 82);
            rootPathLabel.AutoSize = true;

            _rootPathTextBox = new TextBox();
            _rootPathTextBox.Location = new Point(15, 102);
            _rootPathTextBox.Size = new Size(315, 23);

            _browseButton = new Button();
            _browseButton.Text = "Обзор...";
            _browseButton.Location = new Point(338, 101);
            _browseButton.Size = new Size(77, 25);
            _browseButton.Click += OnBrowseClick;

            _okButton = new Button();
            _okButton.Text = "ОК";
            _okButton.Location = new Point(246, 136);
            _okButton.Size = new Size(80, 28);
            _okButton.Click += OnOkClick;

            _cancelButton = new Button();
            _cancelButton.Text = "Отмена";
            _cancelButton.Location = new Point(332, 136);
            _cancelButton.Size = new Size(83, 28);
            _cancelButton.Click += OnCancelClick;

            Controls.Add(markerLabel);
            Controls.Add(_markerTextBox);
            Controls.Add(rootPathLabel);
            Controls.Add(_rootPathTextBox);
            Controls.Add(_browseButton);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите корневую папку (например, папку библиотеки Steam)";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _rootPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_markerTextBox.Text) == true)
            {
                MessageBox.Show(this, "Введите маркер.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_rootPathTextBox.Text) == true)
            {
                MessageBox.Show(this, "Введите или выберите корневую папку.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            KnownRoot result = new KnownRoot();
            result.Marker = _markerTextBox.Text.Trim();
            result.RootPath = _rootPathTextBox.Text.Trim();

            Result = result;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            Result = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
