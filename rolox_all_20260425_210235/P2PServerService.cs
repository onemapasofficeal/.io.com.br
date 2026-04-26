using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoloxApp.Services
{
    public class P2PServerService
    {
        private TcpListener? _listener;
        private readonly List<TcpClient> _clients = new();
        private bool _running;
        public int Port { get; private set; } = 7777;
        public string HostUsername { get; private set; } = "";
        public List<string> ConnectedPlayers { get; } = new();

        public event Action<string>? OnPlayerJoined;
        public event Action<string>? OnPlayerLeft;
        public event Action<string, string>? OnMessageReceived; // (username, msg)

        public async Task StartHostAsync(string hostUsername, int port = 7777)
        {
            // Se já está rodando, para antes de reiniciar
            if (_running) StopHost();

            HostUsername = hostUsername;
            Port = port;
            _running = true;
            ConnectedPlayers.Clear();
            ConnectedPlayers.Add(hostUsername);

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            await Task.Run(AcceptClientsLoop);
        }

        public void StopHost()
        {
            _running = false;
            _listener?.Stop();
            foreach (var c in _clients) c.Close();
            _clients.Clear();
            ConnectedPlayers.Clear();
        }

        private void AcceptClientsLoop()
        {
            while (_running)
            {
                try
                {
                    var client = _listener!.AcceptTcpClient();
                    _clients.Add(client);
                    Task.Run(() => HandleClient(client));
                }
                catch { break; }
            }
        }

        private void HandleClient(TcpClient client)
        {
            string username = "Jogador";
            try
            {
                var stream = client.GetStream();
                var buf = new byte[1024];

                // Primeiro pacote = username
                int n = stream.Read(buf, 0, buf.Length);
                username = Encoding.UTF8.GetString(buf, 0, n).Trim();
                ConnectedPlayers.Add(username);
                OnPlayerJoined?.Invoke(username);
                Broadcast($"[{username} entrou no servidor]", "Sistema");

                while (_running && client.Connected)
                {
                    n = stream.Read(buf, 0, buf.Length);
                    if (n == 0) break;
                    string msg = Encoding.UTF8.GetString(buf, 0, n).Trim();
                    OnMessageReceived?.Invoke(username, msg);
                    Broadcast(msg, username);
                }
            }
            catch { }
            finally
            {
                ConnectedPlayers.Remove(username);
                _clients.Remove(client);
                client.Close();
                OnPlayerLeft?.Invoke(username);
                Broadcast($"[{username} saiu do servidor]", "Sistema");
            }
        }

        public void Broadcast(string message, string from)
        {
            var data = Encoding.UTF8.GetBytes($"{from}: {message}");
            foreach (var c in _clients.ToArray())
            {
                try { c.GetStream().Write(data, 0, data.Length); }
                catch { }
            }
        }

        // Cliente: conectar ao host
        private TcpClient? _clientConn;
        private string _clientUsername = "";

        public async Task<bool> JoinServerAsync(string hostIp, int port, string username)
        {
            try
            {
                _clientUsername = username;
                _clientConn = new TcpClient();
                await _clientConn.ConnectAsync(hostIp, port);
                var stream = _clientConn.GetStream();
                var nameBytes = Encoding.UTF8.GetBytes(username);
                await stream.WriteAsync(nameBytes, 0, nameBytes.Length);
                _ = Task.Run(ReceiveLoop);
                return true;
            }
            catch { return false; }
        }

        private void ReceiveLoop()
        {
            var buf = new byte[1024];
            try
            {
                var stream = _clientConn!.GetStream();
                while (_clientConn.Connected)
                {
                    int n = stream.Read(buf, 0, buf.Length);
                    if (n == 0) break;
                    string msg = Encoding.UTF8.GetString(buf, 0, n);
                    OnMessageReceived?.Invoke("", msg);
                }
            }
            catch { }
        }

        public void SendMessage(string message)
        {
            if (_clientConn == null || !_clientConn.Connected) return;
            var data = Encoding.UTF8.GetBytes(message);
            _clientConn.GetStream().Write(data, 0, data.Length);
        }

        public void Disconnect()
        {
            _clientConn?.Close();
            _clientConn = null;
        }
    }
}
