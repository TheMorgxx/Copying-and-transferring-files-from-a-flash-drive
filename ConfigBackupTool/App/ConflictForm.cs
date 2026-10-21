using System;
using System.Drawing;
using System.Windows.Forms;
using ConfigBackupTool.Managers;

namespace ConfigBackupTool.App
{
    // Вызывается только при RestoreMode.AskOnConflict и реальном расхождении хэша (см.
    // IRestoreConflictResolver.Resolve). currentLastWriteTime/currentFileSize показываются
    // пользователю как подсказка ("текущий файл новее бэкапа" и т.п.), решение всегда за ним.
    public class ConflictForm : Form
    {
        private Button _replaceButton;
        private Button _keepButton;

        public RestoreConflictAction SelectedAction { get; private set; }

        public ConflictForm(string gameName, string targetPath, DateTime currentLastWriteTime, long currentFileSize)
        {
            if (targetPath == null)
            {
                throw new ArgumentNullException("targetPath");
            }

            // Отмена диалога (крестик) равносильна "Оставить" — при неопределённости
            // существующий файл никогда не заменяется молча.
            SelectedAction = RestoreConflictAction.Keep;

            InitializeLayout(gameName, targetPath, currentLastWriteTime, currentFileSize);
        }

        private void InitializeLayout(string gameName, string targetPath, DateTime currentLastWriteTime, long currentFileSize)
        {
            Text = "Конфликт при восстановлении";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 190);

            string infoText =
                "Игра: " + gameName + "\r\n" +
                "Файл: " + targetPath + "\r\n" +
                "Текущий файл изменён: " + currentLastWriteTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") + "\r\n" +
                "Текущий размер: " + FormatSize(currentFileSize) + "\r\n\r\n" +
                "Файл на диске отличается от версии в бэкапе. Заменить его версией из бэкапа?";

            Label infoLabel = new Label();
            infoLabel.Text = infoText;
            infoLabel.Location = new Point(15, 12);
            infoLabel.Size = new Size(430, 130);

            _replaceButton = new Button();
            _replaceButton.Text = "Заменить";
            _replaceButton.Location = new Point(180, 150);
            _replaceButton.Size = new Size(120, 28);
            _replaceButton.Click += OnReplaceClick;

            _keepButton = new Button();
            _keepButton.Text = "Оставить как есть";
            _keepButton.Location = new Point(310, 150);
            _keepButton.Size = new Size(135, 28);
            _keepButton.Click += OnKeepClick;

            Controls.Add(infoLabel);
            Controls.Add(_replaceButton);
            Controls.Add(_keepButton);
        }

        private string FormatSize(long bytes)
        {
            const long kilobyte = 1024;
            const long megabyte = kilobyte * 1024;

            if (bytes >= megabyte)
            {
                return (bytes / (double)megabyte).ToString("0.00") + " МБ";
            }

            if (bytes >= kilobyte)
            {
                return (bytes / (double)kilobyte).ToString("0.00") + " КБ";
            }

            return bytes + " байт";
        }

        private void OnReplaceClick(object sender, EventArgs e)
        {
            SelectedAction = RestoreConflictAction.Replace;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnKeepClick(object sender, EventArgs e)
        {
            SelectedAction = RestoreConflictAction.Keep;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
