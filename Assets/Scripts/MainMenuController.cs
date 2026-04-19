using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject menuPanel;

    public void PlayGame()
    {
        menuPanel.SetActive(false);
        SceneManager.LoadScene("Game"); 
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
