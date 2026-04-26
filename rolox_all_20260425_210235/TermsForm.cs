using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RoloxApp
{
    public class TermsForm : Form
    {
        private static readonly string _acceptedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rolox", "terms_accepted.dat");

        public static bool AlreadyAccepted() => File.Exists(_acceptedPath);

        private static void MarkAccepted()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_acceptedPath)!);
            File.WriteAllText(_acceptedPath, DateTime.Now.ToString());
        }

        public TermsForm()
        {
            Text = "Rolox - Termos de Uso";
            Size = new Size(700, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;

            var logo = new Label
            {
                Text = "ROLOX",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false,
                Size = new Size(660, 45),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 15)
            };

            var lblTitle = new Label
            {
                Text = "Termos de Uso e Política de Privacidade",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(660, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 60)
            };

            var rtb = new RichTextBox
            {
                Location = new Point(20, 95),
                Size = new Size(650, 380),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 9),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            rtb.Text = @"TERMOS DE USO — ROLOX
Versão 1.0 | One Mapas Official

Ao usar o Rolox, você concorda com os seguintes termos:

1. SOBRE O ROLOX
O Rolox é um cliente alternativo desenvolvido pela One Mapas Official.
Os jogos, conteúdos e tecnologia de jogo são de propriedade da Roblox Corporation.
O Rolox não é afiliado, patrocinado ou endossado pela Roblox Corporation.

2. USO PERMITIDO
- Usar o Rolox para acessar jogos do Roblox de forma pessoal e não comercial.
- Hospedar servidores privados gratuitos para uso pessoal.
- Compartilhar o app com amigos para uso pessoal.

3. USO PROIBIDO
- Usar o Rolox para atividades ilegais ou que violem os Termos de Serviço do Roblox.
- Modificar, redistribuir ou vender o Rolox sem autorização da One Mapas Official.
- Usar o Rolox para assediar, prejudicar ou enganar outros usuários.
- Tentar contornar sistemas de segurança do Roblox ou do Rolox.

4. CHAT E MODERAÇÃO
O chat do Rolox possui moderação automática por IA.
Mensagens com conteúdo inapropriado resultam em ban progressivo do chat:
1 minuto → 2 minutos → 4 minutos → e assim por diante.
Conteúdo extremamente inapropriado pode resultar em ban permanente.

5. SERVIDORES PRIVADOS
Os servidores P2P rodam no PC do host. A One Mapas Official não armazena
nem monitora o conteúdo dos servidores privados.
O host é responsável pelo conteúdo e comportamento em seu servidor.

6. PRIVACIDADE E DADOS
O Rolox armazena localmente apenas:
- Seu username do Roblox (para login automático)
- Um token de sessão criptografado
Nenhum dado é enviado para servidores da One Mapas Official.
Os dados do perfil são obtidos da API pública do Roblox.

7. PROPRIEDADE INTELECTUAL
Todos os jogos, avatares, itens e conteúdos do Roblox são propriedade
da Roblox Corporation. O Rolox apenas acessa esses conteúdos via API pública.
O nome ""Rolox"", o logo e o código do cliente são propriedade da One Mapas Official.

8. ISENÇÃO DE RESPONSABILIDADE
O Rolox é fornecido ""como está"", sem garantias de qualquer tipo.
A One Mapas Official não se responsabiliza por:
- Perda de dados ou danos ao sistema
- Interrupções no serviço do Roblox
- Conteúdo gerado por outros usuários em servidores privados

9. ALTERAÇÕES NOS TERMOS
A One Mapas Official pode atualizar estes termos a qualquer momento.
O uso continuado do Rolox após alterações implica aceitação dos novos termos.

10. CONTATO
Para dúvidas ou suporte, acesse a comunidade One Mapas Official no Roblox:
https://www.roblox.com/pt/communities/1005332769/one-mapas-officeal

---
POLÍTICA DE PRIVACIDADE

Dados coletados localmente:
• Username do Roblox
• Token de sessão (criptografado com DPAPI do Windows)

Dados NÃO coletados:
• Senha do Roblox
• Dados de pagamento
• Histórico de jogos
• Mensagens do chat

Ao clicar em ""Aceitar"", você confirma que leu e concorda com estes termos.";

            var chkAccept = new CheckBox
            {
                Text = "Li e aceito os Termos de Uso e a Política de Privacidade",
                Font = new Font("Arial", 9),
                ForeColor = Color.White,
                Location = new Point(20, 485),
                AutoSize = true
            };

            var btnAccept = new Button
            {
                Text = "Aceitar e Continuar",
                Location = new Point(20, 520),
                Size = new Size(300, 42),
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAccept.FlatAppearance.BorderSize = 0;

            var btnDecline = new Button
            {
                Text = "Recusar e Fechar",
                Location = new Point(340, 520),
                Size = new Size(200, 42),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(50, 30, 30),
                ForeColor = Color.OrangeRed,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDecline.FlatAppearance.BorderSize = 0;

            chkAccept.CheckedChanged += (s, e) => btnAccept.Enabled = chkAccept.Checked;

            btnAccept.Click += (s, e) =>
            {
                MarkAccepted();
                DialogResult = DialogResult.OK;
                Close();
            };

            btnDecline.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Application.Exit();
            };

            Controls.AddRange(new Control[] { logo, lblTitle, rtb, chkAccept, btnAccept, btnDecline });
        }
    }
}
