using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject panelMejoras;
    private bool isPaused = false;

    public AudioSource musicManager;
    private CursorManager cursorManager;   

    void Start()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        cursorManager = FindObjectOfType<CursorManager>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.juegoTerminado)
            return;

        if (panelMejoras.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            player.isPaused = true;

        if (cursorManager != null)
            cursorManager.DesactivarCrosshair();

        if (musicManager != null)
            musicManager.Pause();
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            player.isPaused = false;

        if (cursorManager != null)
            cursorManager.ActivarCrosshair();

        if (musicManager != null)
            musicManager.UnPause();        
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
