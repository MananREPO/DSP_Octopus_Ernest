using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public string mainSceneName = "MainScene";
    public string mainMenu = "MainMenu";

    public void PlayGame()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenu);
    }
}