using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Patrón Singleton

    [Header("Referencias")]
    public PlayerHealth playerHealth;
    public GameObject gameOverUI;      // Panel de UI
    public TMP_Text gameOverText;
    public Button returnButton;

    private int enemigosRestantes;
    private bool juegoTerminado = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Buscar y contar enemigos al inicio
        enemigosRestantes = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(VolverAlMenu);
    }

    // Llamado desde EnemyHealth al morir
    public void EnemigoDerrotado()
    {
        if (juegoTerminado) return;

        enemigosRestantes--;
        if (enemigosRestantes <= 0)
        {
            FinalizarJuego("¡Victoria!");
        }
    }

    public void RegistrarEnemigo(GameObject enemigo)
    {
        if (juegoTerminado) return;
        enemigosRestantes++;
    }

    public void DesregistrarEnemigo(GameObject enemigo)
    {
        if (juegoTerminado) return;

        enemigosRestantes--;
        if (enemigosRestantes <= 0)
        {
            FinalizarJuego("¡Victoria!");
        }
    }

    // Llamado desde PlayerHealth al morir
    public void JugadorDerrotado()
    {
        if (juegoTerminado) return;

        FinalizarJuego("Has muerto");
    }

    void FinalizarJuego(string mensaje)
    {
        juegoTerminado = true;
        Time.timeScale = 0f; // pausa todo el juego (movimiento, físicas, etc.)

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            gameOverText.text = mensaje;
        }

        // Opcional: desactivar control del jugador
        if (playerHealth != null)
        {
            var controller = playerHealth.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;
        }
    }

    void VolverAlMenu()
    {
        Time.timeScale = 1f; // restaurar tiempo normal
        SceneManager.LoadScene("MainMenu"); // nombre de tu escena de menú
    }
}
