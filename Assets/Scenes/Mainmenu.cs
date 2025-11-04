using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level0");  // 或直接 "Level1"
    }

    public void QuitGame()
    {
        Application.Quit();  // 编辑器中用 UnityEditor.EditorApplication.isPlaying = false;
    }
}
