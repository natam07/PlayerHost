using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPClient : MonoBehaviour, IClient
{
    [Header("Referencias")]
    public LobbyUIManager lobbyUI;  
    public FPSController playerController;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    public string localPlayerID;

    [Header("Characters")]
    public GameObject rabbitPrefab;
    public GameObject foxPrefab;
    public GameObject localPlayer;
    public Transform spawnFox;
    public Transform spawnRabbit;

    private bool isHost = false;
    bool isGameRunning = false;
    public bool canSendPosition = false;
    private Coroutine positionCoroutine;
    public bool isConnected { get; private set; }

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    Queue<string> messageQueue = new Queue<string>();

    [Header("Dictionaries")]
    private Dictionary<string, System.Diagnostics.Stopwatch> pingTimers = new Dictionary<string, System.Diagnostics.Stopwatch>();
    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    void Start()
    {
        OnMessageReceived += HandleMessageReceived;
    }

    void Update()
    {
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                string msg = messageQueue.Dequeue();
                HandleMessageReceived(msg);
            }
        }
    }

    public async Task ConnectToServer(string ip, int port)
    {
        tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(ip, port);
        networkStream = tcpClient.GetStream();

        isConnected = true;
        Debug.Log("[Client] Connected to server");
        OnConnected?.Invoke();
        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (tcpClient != null && tcpClient.Connected)
            {
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] messages = message.Split('\n');

                foreach (string msg in messages)
                {
                    if (string.IsNullOrWhiteSpace(msg)) continue;

                    Debug.Log("[Client] LLEGÓ LIMPIO: " + msg.Trim());

                    if (msg.StartsWith("ERROR:GAME_ALREADY_STARTED"))
                    {
                        lobbyUI.SetStatus("No puedes unirte, la partida ya ha comenzado.");
                        Disconnect();
                        return;
                    }

                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(msg.Trim());
                    }
                }

            }
        }
        finally
        {
            //Disconnect();
        }
    }
    void HandleMessageReceived(string message)
    {
        if (message.StartsWith("ROLE:"))
        {
            isHost = message.Contains("HOST");
            lobbyUI.SetRole(isHost);
            lobbyUI.SetStatus(isHost ? "Eres el Host" : "Eres Cliente");

            if (isHost)
                PingLoop();
        }
        else if (message.StartsWith("PLAYER:"))
        {
            string playerID = message.Replace("PLAYER:", "");

            if (string.IsNullOrEmpty(localPlayerID))
            {
                localPlayerID = playerID.Trim();
                Debug.Log("[Client] Mi ID es: " + localPlayerID);
                lobbyUI.SetStatus($"Tu ID es {playerID}");
                return;
            }

            if (!remotePlayers.ContainsKey(playerID))
            {
                lobbyUI.AddPlayer(playerID, 0);
            }
        }
        else if (message.StartsWith("PINGVAL:"))
        {
            string nombre = message.Replace("PINGVAL:", "");

            if (pingTimers.ContainsKey(nombre))
            {
                var sw = pingTimers[nombre];
                sw.Stop();
                int ping = (int)sw.ElapsedMilliseconds;

                lobbyUI.UpdatePlayerPing(nombre, ping);
                pingTimers.Remove(nombre);
            }
        }
        else if (message.StartsWith("KICK:"))
        {
            string nombre = message.Replace("KICK:", "");
            Debug.LogWarning("[Client] Expulsado por el host");

            if (lobbyUI != null)
            {
                lobbyUI.SetStatus($"Has sido expulsado del juego ({nombre})");
                lobbyUI.lobbyPanel.SetActive(true);
                lobbyUI.gamePanel.SetActive(false);
            }

            Disconnect();
        }

        else if (message == "PAUSE")
        {
            lobbyUI.SetStatus("Juego pausado por el host");
            if (lobbyUI.pausePanel != null)
                lobbyUI.pausePanel.SetActive(true);
        }
        else if (message == "RESUME")
        {
            lobbyUI.SetStatus("Juego reanudado por el host");
            if (lobbyUI.pausePanel != null)
                lobbyUI.pausePanel.SetActive(false);
        }
        else if (message.StartsWith("REMOVE:"))
        {
            string nombre = message.Replace("REMOVE:", "");
            if (lobbyUI != null)
            {
                lobbyUI.RemovePlayer(nombre);
                lobbyUI.SetStatus($"Jugador {nombre} salió del juego");
            }
        }
        else if (message.StartsWith("PLAY:"))
        {
            string role = message.Replace("PLAY:", "");

            lobbyUI.lobbyPanel.SetActive(false);
            lobbyUI.gamePanel.SetActive(true);

            if (isHost)
            {
                CreateHostPlayer(role);
            }
            else
            {
                if (role == "FOX") SetRoleFox();
                else SetRoleRabbit();
            }

            lobbyUI.ShowRole(role);

            canSendPosition = true;

            if (positionCoroutine == null)
            {
                positionCoroutine = StartCoroutine(PositionUpdateLoop());
                Debug.Log("[Client] Corrutina iniciada");
            }

            SendInitialPosition();
        }
        else if (message.StartsWith("POS:"))
        {
            string[] parts = message.Split(':');

            string playerId = parts[1].Trim();
            float x = float.Parse(parts[2]);
            float y = float.Parse(parts[3]);
            float z = float.Parse(parts[4]);
            string role = parts[5];

            Vector3 pos = new Vector3(x, y, z);

            Debug.Log($"[Client] POS recibido de {playerId}, yo soy {localPlayerID}");

            if (playerId != localPlayerID)
            {
                if (!remotePlayers.ContainsKey(playerId) || remotePlayers[playerId] == null)
                {
                    Debug.Log("[Client] Creando jugador remoto: " + playerId);

                    GameObject prefab = (role == "FOX") ? foxPrefab : rabbitPrefab;
                    GameObject newPlayer = Instantiate(prefab, pos, Quaternion.identity);

                    Camera cam = newPlayer.GetComponentInChildren<Camera>();
                    if (cam != null) cam.enabled = false;

                    var remoteScript = newPlayer.AddComponent<RemotePlayer>();
                    remoteScript.targetPosition = pos;

                    remotePlayers[playerId] = newPlayer;
                }
                else
                {
                    var remoteScript = remotePlayers[playerId].GetComponent<RemotePlayer>();
                    remoteScript.SetTargetPosition(pos);
                }
            }
        }
        else if (message == "GAME_OVER")
        {
            Debug.Log("[Client] GAME OVER recibido");

            if (lobbyUI != null)
            {
                lobbyUI.ShowGameOver();
            }
        }
        else
        {
            Debug.Log("[Client] Mensaje: " + message);
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!isConnected || networkStream == null)
        {
            Debug.Log("[Client] Not connected to server");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        await networkStream.WriteAsync(data, 0, data.Length);

        Debug.Log("[Client] Sent: " + message);
    }

    private async void PingLoop()
    {
        while (isConnected)
        {
            if (isGameRunning)
            {
                await Task.Delay(1000);
                continue;
            }

            if (lobbyUI != null)
            {
                foreach (Transform child in lobbyUI.playersContent)
                {
                    var texts = child.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                    if (texts.Length >= 2)
                    {
                        string playerName = texts[0].text;

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        pingTimers[playerName] = sw;

                        await SendMessageAsync("PING:" + playerName);
                    }
                }
            }

            await Task.Delay(5000);
        }
    }

    void SendPosition()
    {
        if (string.IsNullOrEmpty(localPlayerID) || localPlayer == null) return;

        Vector3 pos = localPlayer.transform.position;

        string message = $"POS_UPDATE:{localPlayerID}:{pos.x}:{pos.y}:{pos.z}";

        Debug.Log("[Client] Enviando: " + message);

        _ = SendMessageAsync(message);
    }
    IEnumerator PositionUpdateLoop()
    {
        Debug.Log("[Client] LOOP INICIADO");

        while (true)
        {
            Debug.Log("[Client] LOOP VIVO");

            if (canSendPosition && localPlayer != null)
            {
                Debug.Log("[Client] Intentando enviar posición");
                SendPosition();
            }
            else
            {
                Debug.Log("[Client] NO puede enviar posición");
            }

            yield return new WaitForSeconds(0.05f);
        }
    }
    private void SendInitialPosition()
    {
        if (localPlayer != null)
        {
            Vector3 pos = localPlayer.transform.position;
            string message = $"POS_UPDATE:{localPlayerID}:{pos.x}:{pos.y}:{pos.z}";
            Debug.Log("[Client] Enviando posición inicial: " + message);
            _ = SendMessageAsync(message);
        }
    }

    public void CreateHostPlayer(string role)
    {
        if (role == "FOX")
        {
            localPlayer = Instantiate(foxPrefab, spawnFox.position, Quaternion.identity);
            playerController = localPlayer.GetComponent<FPSController>();
            playerController.SetRoleFox();
            lobbyUI.SetStatus("Eres el Zorro (Host)");
        }
        else
        {
            localPlayer = Instantiate(rabbitPrefab, spawnRabbit.position, Quaternion.identity);
            playerController = localPlayer.GetComponent<FPSController>();
            playerController.SetRoleRabbit();
            lobbyUI.SetStatus("Eres el Conejo (Host)");
        }
        Camera cam = localPlayer.GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;

        StartCoroutine(PositionUpdateLoop());
    }

    private void SetRoleFox()
    {
        if (localPlayer != null) Destroy(localPlayer);
        localPlayer = Instantiate(foxPrefab, spawnFox.position, Quaternion.identity);
        playerController = localPlayer.GetComponent<FPSController>();
        lobbyUI.SetStatus("Eres el Zorro");
        playerController.SetRoleFox();

        Camera cam = localPlayer.GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;

        SendInitialPosition();
        StartCoroutine(PositionUpdateLoop());
    }

    private void SetRoleRabbit()
    {
        if (localPlayer != null) Destroy(localPlayer);
        localPlayer = Instantiate(rabbitPrefab, spawnRabbit.position, Quaternion.identity);
        playerController = localPlayer.GetComponent<FPSController>();
        lobbyUI.SetStatus("Eres un Conejo");
        playerController.SetRoleRabbit();

        Camera cam = localPlayer.GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;
        SendInitialPosition();
        StartCoroutine(PositionUpdateLoop());
    }
    public void Disconnect()
    {
        isConnected = false;
        networkStream?.Close();
        tcpClient?.Close();

        networkStream = null;
        tcpClient = null;

        OnDisconnected?.Invoke();
        Debug.Log("[Client] Disconnected");
    }
    private async void OnDestroy()
    {
        //Disconnect();
        await Task.Delay(50);
    }
}
