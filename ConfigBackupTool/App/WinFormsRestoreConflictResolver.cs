using System;
using System.Windows.Forms;
using ConfigBackupTool.Managers;

namespace ConfigBackupTool.App
{
    // Мост между RestoreManager (работает в фоновом Task.Run) и модальным ConflictForm
    // (может показываться только на UI-потоке). Resolve вызывается синхронно из фонового
    // потока и должен вернуть значение немедленно — поэтому здесь Control.Invoke, а не
    // Progress<T> (тот годится только для одностороннего "выстрелил и забыл" лога).
    public class WinFormsRestoreConflictResolver : IRestoreConflictResolver
    {
        private readonly Control _owner;

        public WinFormsRestoreConflictResolver(Control owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            _owner = owner;
        }

        public RestoreConflictAction Resolve(string gameName, string targetPath, DateTime currentLastWriteTime, long currentFileSize)
        {
            Func<RestoreConflictAction> showDialog = delegate
            {
                using (ConflictForm form = new ConflictForm(gameName, targetPath, currentLastWriteTime, currentFileSize))
                {
                    form.ShowDialog(_owner);
                    return form.SelectedAction;
                }
            };

            if (_owner.InvokeRequired == true)
            {
                return (RestoreConflictAction)_owner.Invoke(showDialog);
            }

            return showDialog();
        }
    }
}
