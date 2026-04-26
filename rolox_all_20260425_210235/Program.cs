using System;
using System.Windows.Forms;

namespace RoloxStudio
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.ToString(), "Erro — RoloxStudio",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro ao Iniciar — RoloxStudio",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
