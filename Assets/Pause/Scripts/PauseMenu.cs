using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button resume;
    [SerializeField] private Button settings;
    [SerializeField] private Button journal;
    [SerializeField] private Button exit;
    [SerializeField] private Canvas canvas;
    [SerializeField] private string mainMenuScene;
    
    private void Start()
    {
        resume.onClick.AddListener(Resume);
        exit.onClick.AddListener(Quit);
        journal.onClick.AddListener(OpenJournal);
        InputManager.Instance.PauseClicked += PauseGame;
    }

    private void Pause()
    {
        canvas.enabled = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void Resume()
    {
        canvas.enabled = false;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PauseGame()
    {
        if (Time.timeScale == 0)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private void Quit()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    private void OpenJournal()
    {
        JournalUI.Instance.OpenJournal();
    }
}
