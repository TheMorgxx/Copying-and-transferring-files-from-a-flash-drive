using System;
using System.Drawing;
using System.Windows.Forms;

namespace ConfigBackupTool.App
{
    // Простой модальный диалог "заголовок + подсказка + одна строка ввода". Используется
    // там, где нужен один текстовый ответ пользователя (сейчас — название игры при
    // добавлении/переименовании в SettingsForm). Проверяет только непустоту ввода —
    // проверку уникальности (например, дублирующееся имя игры) делает вызывающий код,
    // у которого есть доступ к остальному списку.
    public class TextInputForm : Form
    {
        private TextBox _inputTextBox;
        private Button _okButton;
        private Button _cancelButton;

        public string InputText { get; private set; }

        public TextInputForm(string title, string prompt, string initialValue)
        {
            InitializeLayout(title, prompt);

            if (initialValue != null)
            {
                _inputTextBox.Text = initialValue;
            }
        }

        private void InitializeLayout(string title, string prompt)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 130);

            Label promptLabel = new Label();
            promptLabel.Text = prompt;
            promptLabel.Location = new Point(15, 12);
            promptLabel.AutoSize = true;

            _inputTextBox = new TextBox();
            _inputTextBox.Location = new Point(15, 36);
            _inputTextBox.Size = new Size(330, 23);

            _okButton = new Button();
            _okButton.Text = "ОК";
            _okButton.Location = new Point(176, 80);
            _okButton.Size = new Size(80, 28);
            _okButton.Click += OnOkClick;

            _cancelButton = new Button();
            _cancelButton.Text = "Отмена";
            _cancelButton.Location = new Point(262, 80);
            _cancelButton.Size = new Size(83, 28);
            _cancelButton.Click += OnCancelClick;

            Controls.Add(promptLabel);
            Controls.Add(_inputTextBox);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_inputTextBox.Text) == true)
            {
                MessageBox.Show(this, "Введите значение.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            InputText = _inputTextBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            InputText = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
