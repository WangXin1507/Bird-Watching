using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Name of the play scene
    /// </summary>
    public string PlayScene;

    public void Play()
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(PlayScene));
    }

    public void Quit()
    {
        Application.Quit();
    }
}
