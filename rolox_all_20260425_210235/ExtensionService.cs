using System;
using System.IO;
using System.IO.Compression;

namespace RoloxApp.Services
{
    /// <summary>
    /// Empacota e instala a extensão do Chrome/Edge automaticamente.
    /// </summary>
    public static class ExtensionService
    {
        private static string ExtensionSourcePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extension");

        private static string ExtensionZipPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rolox-extension.zip");

        public static bool ExtensionExists() =>
            Directory.Exists(ExtensionSourcePath) &&
            File.Exists(Path.Combine(ExtensionSourcePath, "manifest.json"));

        /// <summary>
        /// Abre o Chrome/Edge na página de extensões com a pasta da extensão pronta para carregar.
        /// O usuário clica em "Carregar sem compactação" e seleciona a pasta.
        /// </summary>
        public static void OpenInstallGuide()
        {
            if (!ExtensionExists()) return;

            // Abre Edge na página de extensões
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "msedge",
                    Arguments       = "edge://extensions/",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback Chrome
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = "chrome",
                        Arguments       = "chrome://extensions/",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        public static string GetExtensionPath() => ExtensionSourcePath;
    }
}
