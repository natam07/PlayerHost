using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPServer : MonoBehaviour, IServer
{
    private TcpListener tcpListener;
    private List<TcpClient> connectedClients = new List<TcpClient>();
    private Dictionary<TcpClient, string> playerMap = new Dictionary<TcpClient, string>();
    public bool gameStarted = false;
    private int playerCounter = 1;

    private HashSet<string> announcedPlayers = new HashSet<string>();

    public bool isServerRunning { get; private set; }

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    private Dictionary<string, Vector3> playerPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, string> playerRoles = new Dictionary<string, string>();

    private TcpClient hostClient;
    public async Task StartServer(int port)
    {
        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        isServerRunning = true;

        Debug.Log("[Server] Server started, waiting for connections...");

        _ = AcceptClientsLoop();
    }

    private async Task AcceptClientsLoop()
    {
        while (isServerRunning)
        {
            TcpClient newClient = await tcpListener.AcceptTcpClientAsync();

            if (gameStarted)
            {
                Debug.Log("[Server] Conexión rechazada: partida ya iniciada");
                await SendMessageToClientAsync(newClient, "ERROR:GAME_ALREADY_STARTED");
                newClient.Close();
                continue;
            }
            string remoteEndPoint = newClient.Client.RemoteEndPoint.ToString();
            bool yaExiste = connectedClients.Any(c => c.Client.RemoteEndPoint.ToString() == remoteEndPoint);

            if (yaExiste)
            {
                Debug.LogWarning("[Server] Conexión duplicada detectada, ignorando: " + remoteEndPoint);
                continue;
            }

            connectedClients.Add(newClient);

            string playerID = $"Player{playerCounter++}";
            playerMap[newClient] = playerID;

            Debug.Log("[Server] Client connected: " + remoteEndPoint);
            OnConnected?.Invoke();

            _ = ReceiveLoop(newClient);

            if (connectedClients.Count == 1)
            {
                hostClient = newClient;
                await SendMessageToClientAsync(newClient, "ROLE:HOST");
            }
            else
            {
                await SendMessageToClientAsync(newClient, "ROLE:CLIENT");
            }

            // Enviar ID al cliente nuevo
            await SendMessageToClientAsync(newClient, $"PLAYER:{playerID}");
            Debug.Log($"[Server] ID asignado a nuevo cliente: {playerID}");

            // Avisar a los demás
            foreach (var client in connectedClients)
            {
                if (client.Connected)
                {
                    await SendMessageToClientAsync(client, $"PLAYER:{playerID}");
                }
            }
        }
    }


    private async Task ReceiveLoop(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] messages = message.Split('\n');

                foreach (string msg in messages)
                {
                    if (string.IsNullOrWhiteSpace(msg)) continue;

                    Debug.Log("[Server] LLEGÓ LIMPIO: " + msg.Trim());

                    HandleServerMessage(client, msg.Trim());
                }
                OnMessageReceived?.Invoke(message);
            }
        }
        finally
        {
            if (playerMap.ContainsKey(client))
            {
                string playerID = playerMap[client];
                playerMap.Remove(client);

                await BroadcastMessageAsync($"REMOVE:{playerID}");
                Debug.Log($"[Server] Jugador {playerID} desconectado y removido");
            }
            connectedClients.Remove(client);
            client.Close();
            OnDisconnected?.Invoke();
            Debug.Log("[Server] Client disconnected");
        }
    }


    private async Task SendMessageToClientAsync(TcpClient client, string message)
    {
        if (client == null || !client.Connected) return;

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        await client.GetStream().WriteAsync(data, 0, data.Length);
    }

    public async Task SendMessageAsync(string message)
    {
        foreach (var client in connectedClients)
        {
            if (client.Connected)
                await SendMessageToClientAsync(client, message);
        }
    }

    public async Task BroadcastMessageAsync(string message)
    {
        foreach (var client in connectedClients)
        {
            if (client.Connected)
                await SendMessageToClientAsync(client, message);
        }
    }

    void HandleServerMessage(TcpClient client, string message)
    {
        if (message.StartsWith("PING:"))
        {
            string playerID = message.Replace("PING:", "");

            if (hostClient != null && hostClient.Connected)
            {
                _ = SendMessageToClientAsync(hostClient, $"PINGVAL:{playerID}");
            }
        }
        else if (message.StartsWith("POS_UPDATE:"))
        {
            string[] parts = message.Split(':');

            string playerID = parts[1];
            float x = float.Parse(parts[2]);
            float y = float.Parse(parts[3]);
            float z = float.Parse(parts[4]);

            playerPositions[playerID] = new Vector3(x, y, z);

            Debug.Log($"[Server] Guardando posición de {playerID}: {x},{y},{z}");
        }
        else if (message == "GAME_OVER")
    {
        Debug.Log("[Server] GAME OVER recibido (debug)");
    }
        else
        {
            _ = BroadcastMessageAsync($"Echo: {message}");
        }
    }

    public async Task PauseGame()
    {
        await BroadcastMessageAsync("PAUSE");
        Debug.Log("[Server] Juego pausado");
    }

    public async Task ResumeGame()
    {
        await BroadcastMessageAsync("RESUME");
        Debug.Log("[Server] Juego reanudado");
    }

    public async Task KickPlayer(string playerID)
    {
        var clientToKick = playerMap.FirstOrDefault(x => x.Value == playerID).Key;
        if (clientToKick != null)
        {
            await SendMessageToClientAsync(clientToKick, $"KICK:{playerID}");
            clientToKick.Close();
            connectedClients.Remove(clientToKick);
            playerMap.Remove(clientToKick);

            Debug.Log($"[Server] Jugador {playerID} expulsado");
            OnDisconnected?.Invoke();
        }
    }
    public async Task StartGame()
    {
        gameStarted = true;
        await AssignRolesAsync();
        await BroadcastMessageAsync("START_GAME");
        Debug.Log("[Server] Iniciando partida, bloqueando nuevas conexiones");

        _ = BroadcastPositionsLoop();
    }
    private async Task BroadcastPositionsLoop()
    {
        while (isServerRunning && gameStarted)
        {
            string foxID = null;

            foreach (var kvp in playerRoles)
            {
                if (kvp.Value == "FOX")
                {
                    foxID = kvp.Key;
                    break;
                }
            }

            if (foxID != null && playerPositions.ContainsKey(foxID))
            {
                Vector3 foxPos = playerPositions[foxID];

                foreach (var kvp in playerRoles)
                {
                    if (kvp.Value == "RABBIT")
                    {
                        string rabbitID = kvp.Key;

                        if (!playerPositions.ContainsKey(rabbitID))
                            continue;

                        float dist = Vector3.Distance(foxPos, playerPositions[rabbitID]);


                        if (dist < 2f) 
                        {
                            Debug.Log("[Server] GAME OVER - Zorro atrapó conejo");

                            await BroadcastMessageAsync("GAME_OVER");

                            gameStarted = false;
                            return;
                        }
                    }
                }
            }


            foreach (var kvp in playerPositions)
            {
                string playerID = kvp.Key;
                Vector3 pos = kvp.Value;

                if (!playerRoles.TryGetValue(playerID, out string role))
                    continue;

                string posMessage = $"POS:{playerID}:{pos.x}:{pos.y}:{pos.z}:{role}";

                foreach (var client in connectedClients)
                {
                    if (client.Connected)
                        await SendMessageToClientAsync(client, posMessage);
                }
            }

            Debug.Log($"[Server] Enviando {playerPositions.Count} jugadores");

            await Task.Delay(50);
        }
    }



    public async Task AssignRolesAsync()
    {
        Debug.Log("[Server] Asignando roles...");

        if (connectedClients.Count == 0)
        {
            Debug.LogError("[Server] No hay clientes conectados");
            return;
        }

        var allPlayers = connectedClients.ToList();

        var random = new System.Random();
        int index = random.Next(allPlayers.Count);
        var foxClient = allPlayers[index];

        foreach (var client in allPlayers)
        {
            string playerID = playerMap[client];

            if (client == foxClient)
            {
                playerRoles[playerID] = "FOX";
                await SendMessageToClientAsync(client, "PLAY:FOX");
            }
            else
            {
                playerRoles[playerID] = "RABBIT";
                await SendMessageToClientAsync(client, "PLAY:RABBIT");
            }
        }
    }

    public void Disconnect()
    {
        foreach (var client in connectedClients)
        {
            try
            {
                client.GetStream()?.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Server] Error cerrando cliente: " + ex.Message);
            }
        }

        connectedClients.Clear();
        tcpListener?.Stop();
        isServerRunning = false;

        Debug.Log("[Server] Disconnected");
        OnDisconnected?.Invoke();
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(50);
    }
}