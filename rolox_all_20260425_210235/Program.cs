using System;
using System.Windows.Forms;

namespace RoloxLife
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string? saved = SessionStore.Load();
            if (!string.IsNullOrEmpty(saved))
                Application.Run(new MainForm(saved));
            else
                Application.Run(new LoginForm());
        }
    }
}
