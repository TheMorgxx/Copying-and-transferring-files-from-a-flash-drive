using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ConfigBackupTool.App
{
    // Показывается, когда PathResolver нашёл больше одного кандидата для одного и того же
    // пути (например, игра установлена в двух местах на разных дисках). Автоматический выбор
    // "самого нового/большого" не делается никогда (см. п. "Порядок разрешения пути" в
    // README_ARCHITECTURE.md) — решение всегда за пользователем.
    public class AmbiguityForm : Form
    {
        private ListBox _candidatesListBox;
        private Button _okButton;
        private Button _cancelButton;

        public string SelectedPath { get; private set; }

        public AmbiguityForm(IReadOnlyList<string> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException("candidates");
            }

            InitializeLayout();

            for (int i = 0; i < candidates.Count; i++)
            {
                _candidatesListBox.Items.Add(candidates[i]);
            }

            if (_candidatesListBox.Items.Count > 0)
            {
                _candidatesListBox.SelectedIndex = 0;
            }
        }

        private void InitializeLayout()
        {
            Text = "Найдено несколько вариантов";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 260);

            Label hintLabel = new Label();
            hintLabel.Text = "Найдено несколько подходящих папок. Выберите нужную:";
            hintLabel.Location = new Point(15, 12);
            hintLabel.AutoSize = true;

            _candidatesListBox = new ListBox();
            _candidatesListBox.Location = new Point(15, 36);
            _candidatesListBox.Size = new Size(430, 175);
            _candidatesListBox.HorizontalScrollbar = true;

            _okButton = new Button();
            _okButton.Text = "Выбрать";
            _okButton.Location = new Point(260, 222);
            _okButton.Size = new Size(90, 28);
            _okButton.Click += OnOkClick;

            _cancelButton = new Button();
            _cancelButton.Text = "Отмена";
            _cancelButton.Location = new Point(355, 222);
            _cancelButton.Size = new Size(90, 28);
            _cancelButton.Click += OnCancelClick;

            Controls.Add(hintLabel);
            Controls.Add(_candidatesListBox);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (_candidatesListBox.SelectedItem == null)
            {
                MessageBox.Show(this, "Выберите один из вариантов.", "ConfigBackupTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedPath = (string)_candidatesListBox.SelectedItem;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            SelectedPath = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
