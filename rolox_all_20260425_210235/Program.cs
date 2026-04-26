using System;
using System.Windows.Forms;
using KidsCord.Forms;
using KidsCord.Models;
using KidsCord.Services;

namespace KidsCord;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Registra no startup do Windows (silencioso, só na primeira vez)
        if (!StartupService.EstaRegistrado())
            StartupService.Registrar();

        // Tenta carregar perfil salvo
        var perfil = PerfilManager.Carregar();

        if (perfil != null && !string.IsNullOrWhiteSpace(perfil.Nome))
        {
            // Perfil já existe — entra direto
            Application.Run(new MainForm(perfil.Nome, perfil.AvatarIndex));
        }
        else
        {
            // Primeiro acesso — mostra tela de login
            using var login = new LoginForm();
            if (login.ShowDialog() == DialogResult.OK)
            {
                PerfilManager.Salvar(login.NomeUsuario, login.AvatarIndex);
                Application.Run(new MainForm(login.NomeUsuario, login.AvatarIndex));
            }
        }
    }
}
