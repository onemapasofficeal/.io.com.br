using FriendEditor.Forms;

namespace FriendEditor;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
            MessageBox.Show(e.Exception.ToString(), "Erro — Friend Editor",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
