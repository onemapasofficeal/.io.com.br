using System.Windows.Forms;
namespace RoloxBuilder
{
    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new BuilderForm());
        }
    }
}
