using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;

public class ConnectionManager : MonoBehaviour
{
    [Header("Referencias")]
    public LobbyUIManager lobbyUI;
    public TCPServer tcpServer;
    public TCPClient tcpClient;

    [Header("Red")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5555;

    async void Start()
    {
        try
        {
            await tcpClient.ConnectToServer(serverIP, serverPort); 
            lobbyUI.SetRole(false);
            lobbyUI.SetStatus("Conectado al host");
        }
        catch (SocketException)
        {
            await tcpServer.StartServer(serverPort);
            lobbyUI.SetRole(true);
            lobbyUI.SetStatus("Esperando jugadores...");

            await tcpClient.ConnectToServer(serverIP, serverPort);
        }

        tcpServer.OnMessageReceived += HandleServerMessage;
        tcpServer.OnDisconnected += () => lobbyUI.SetStatus("Jugador desconectado");
    }

    private void HandleServerMessage(string msg)
    {
        if (msg.StartsWith("PLAYER:"))
        {
            string nombre = msg.Replace("PLAYER:", "");
            lobbyUI.AddPlayer(nombre, 0);
        } 
        else if (msg.StartsWith("ROLE:"))
        {
            string role = msg.Replace("ROLE:", "");
            if (role == "HOST")
            {
                lobbyUI.SetRole(true);
                lobbyUI.SetStatus("Eres el Host");
            }
            else if (role == "CLIENT")
            {
                lobbyUI.SetRole(false);
                lobbyUI.SetStatus("Eres Cliente");
            }
        }
    }

    public void KickPlayer(string playerID)
    {
        if (tcpServer != null && tcpServer.isServerRunning)
        {
            _ = tcpServer.KickPlayer(playerID);
            lobbyUI.SetStatus($"Jugador {playerID} expulsado");
            lobbyUI.RemovePlayer(playerID);
        }
    }

    public void PauseGame()
    {
        if (tcpServer != null && tcpServer.isServerRunning)
        {
            _ = tcpServer.BroadcastMessageAsync("PAUSE");
            lobbyUI.SetStatus("Juego pausado por el host");
        }
    }

    public void ResumeGame()
    {
        if (tcpServer != null && tcpServer.isServerRunning)
        {
            _ = tcpServer.BroadcastMessageAsync("RESUME");
            lobbyUI.SetStatus("Juego reanudado por el host");
        }
    }

    public async void StartGame()
    {
        if (tcpServer != null && tcpServer.isServerRunning)
        {
            await tcpServer.StartGame();
        }
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
