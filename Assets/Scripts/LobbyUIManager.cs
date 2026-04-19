using UnityEngine;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Transform playersContent;
    public Transform playersContentGame;
    public GameObject playerEntryPrefab;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI roleGameText;

    [Header("Controles por rol")]
    public GameObject hostPanel;
    public GameObject clientPanel;
    public GameObject hostGameControls;
    public GameObject hostPauseControls;
    public GameObject clientGameControls;

    [Header("Paneles de juego")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    public ConnectionManager connectionManager;

    public void AddPlayer(string playerName, int ping)
    {
        foreach (Transform child in playersContent)
        {
            var texts = child.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2 && texts[0].text == playerName)
            {
                Debug.LogWarning($"[LobbyUI] Jugador {playerName} ya existe en la lista");
                return;
            }
        }

        GameObject entryLobby = Instantiate(playerEntryPrefab, playersContent);
        ConfigureEntry(entryLobby, playerName, ping);

        if (playersContentGame != null)
        {
            GameObject entryGame = Instantiate(playerEntryPrefab, playersContentGame);
            ConfigureEntry(entryGame, playerName, ping);
        }

  
    }
    private void ConfigureEntry(GameObject entry, string playerName, int ping)
    {
        var entryTexts = entry.GetComponentsInChildren<TextMeshProUGUI>();
        if (entryTexts.Length >= 2)
        {
            entryTexts[0].text = playerName;
            entryTexts[1].text = $"Ping: {ping} ms";
        }

        var kickButton = entry.GetComponentInChildren<UnityEngine.UI.Button>();
        if (kickButton != null)
        {
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(() => OnKickPlayer(playerName));
        }
    }

    public void UpdatePlayerPing(string playerName, int ping)
    {
        foreach (Transform content in new[] { playersContent, playersContentGame })
        {
            if (content == null) continue;

            foreach (Transform child in content)
            {
                var texts = child.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2 && texts[0].text == playerName)
                {
                    texts[1].text = $"Ping: {ping} ms";
                    break;
                }
            }
        }
    }


    public void RemovePlayer(string playerName)
    {
        foreach (Transform content in new[] { playersContent, playersContentGame })
        {
            if (content == null) continue;

            foreach (Transform child in content)
            {
                var texts = child.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2 && texts[0].text == playerName)
                {
                    Destroy(child.gameObject);
                    break;
                }
            }
        }
    }


    public void SetRole(bool isHost)
    {
        roleText.text = isHost ? "Rol: Host" : "Rol: Cliente";

        if (hostPanel != null) hostPanel.SetActive(isHost);
        if (clientPanel != null) clientPanel.SetActive(!isHost);
        if (hostGameControls != null) hostGameControls.SetActive(isHost);
        if (hostPauseControls != null) hostPauseControls.SetActive(isHost);
        if (clientGameControls != null) clientGameControls.SetActive(!isHost);

    }

    public void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void OnKickPlayer(string playerName)
    {
        connectionManager.KickPlayer(playerName);
    }

    public void OnPauseGame()
    {
        connectionManager.PauseGame();
    }

    public void OnResumeGame()
    {
        connectionManager.ResumeGame();
    }
    public void OnStartGameButton()
    {
        connectionManager.StartGame();
    }
    public void ShowGamePanel()
    {
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);
    }

    public void ShowRole(string role)
    {
        if (roleGameText != null)
            roleGameText.text = $"Tu rol: {role}";
    }
    public void ShowGameOver()
    {
        Debug.Log("[UI] Mostrando panel de fin de juego");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.Log("ERROR: gameOverPanel no asignado");
    }
    public void OnExitButton()
    {
        if (connectionManager != null && connectionManager.tcpClient != null)
        {
            connectionManager.tcpClient.Disconnect();
            SetStatus("Has salido del juego");
            lobbyPanel.SetActive(true);
            gamePanel.SetActive(false);
        }
    }

}
