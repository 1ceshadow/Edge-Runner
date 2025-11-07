using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level0");
    }

    public void QuitGame()
    {
        Application.Quit();  // 编辑器中用 UnityEditor.EditorApplication.isPlaying = false;
    }
}
