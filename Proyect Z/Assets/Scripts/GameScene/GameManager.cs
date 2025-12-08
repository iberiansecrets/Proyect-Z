using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Patrón Singleton

    [Header("Referencias")]
    public PlayerHealth playerHealth;
    public EnemiesSpawner enemiesSpawner; //Spawn de enemigos
    public GameObject gameOverUI;      // Panel de UI
    public TMP_Text gameOverText;
    public TMP_Text rondaText; // Texto de la ronda actual en pantalla
    public TMP_Text timerText; // Texto del temporizador total
    public Button returnButton;    
    public TMP_Text enemigosRestantesText; // Texto de enemigos restantes

    [Header("Rondas")]
    public int rondaActual = 1;
    public int enemigosPorRonda = 7;
    public float dificultad = 1.6f; // Aumenta el número de enemigos por ronda
    public int maxRondas = 10; // Máximo de rondas del juego

    private int enemigosRestantes;
    private bool rondaActiva = false;
    public bool juegoTerminado = false;

    [Header ("Temporizador")]
    public float tiempoTotal = 600f; // 10 minutos
    private bool temporizadorActivo = true;

    [Header("Mejoras")]
    public GameObject mejorasUI; // Panel con los botones de mejoras
    public Button[] botonesMejoras; // Array de 3 botones para las mejoras
    private string[] mejoras = new string[]
    {
        "Salud",
        "Velocidad",
        "Empuje",
        "Daño",
        "Daño de Empuje"
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Inicialización de las interfaces
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (mejorasUI != null)
            mejorasUI.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(VolverAlMenu);

        IniciarRonda(); // Inicia la primera ronda
    }

    void Update()
    {
        // Actualizar temporizador
        if (temporizadorActivo && !juegoTerminado)
        {
            tiempoTotal -= Time.deltaTime;
            ActualizarTimerUI();

            if (tiempoTotal <= 0)
            {
                tiempoTotal = 0;
                temporizadorActivo = false;
                FinalizarJuego("¡Se acabó el tiempo!");
            }
        }

        ActualizarEnemigosUI();
    }

    private void ActualizarTimerUI()
    {
        if (timerText == null) return;

        int minutos = Mathf.FloorToInt(tiempoTotal / 60);
        int segundos = Mathf.FloorToInt(tiempoTotal % 60);
        timerText.text = $"{minutos:00}:{segundos:00}";
    }

    private void ActualizarEnemigosUI()
    {
        if (enemigosRestantesText != null)
        {
            enemigosRestantesText.text = $"Zombies restantes: {enemigosRestantes/2}";
        }            
    }

    void IniciarRonda()
    {
        Debug.Log($"Iniciando ronda {rondaActual}");

        ActualizarEnemigosUI();

        // Calcula la cantidad de enemigos en función de la dificultad
        enemigosRestantes = enemigosPorRonda;

        // Actualiza el texto de ronda
        if (rondaText != null)
            rondaText.text = $"Ronda: {rondaActual}";

        // Genera los enemigos de la oleada
        if (enemiesSpawner != null)
            enemiesSpawner.GenerarOleada(enemigosRestantes, rondaActual);

        rondaActiva = true;
        juegoTerminado = false;
        temporizadorActivo = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        FindObjectOfType<CursorManager>().ActivarCrosshair();
    }

    void AcabarRonda()
    {
        Debug.Log($"Ronda {rondaActual} acabada!");
        rondaActiva = false;
        Time.timeScale = 0f;
        temporizadorActivo = false;
        float random = Random.Range(3, 5 * dificultad);
        enemigosPorRonda += (int)random;
        FindObjectOfType<CursorManager>().DesactivarCrosshair();
        // Si se completan todas las rondas, el jugador gana
        if (rondaActual >= maxRondas)
        {
            FinalizarJuego("¡Has ganado!");
            return;
        }

        // Muestra el menú de mejoras
        if (mejorasUI != null)
        {
            mejorasUI.SetActive(true);
            OpcionesMejoras();
        }
    }

    // Genera 3 mejoras aleatorias de 5 posibles
    void OpcionesMejoras()
    {
        List<string> opciones = new List<string>(mejoras);
        for (int i = 0; i < botonesMejoras.Length; i++)
        {
            if (opciones.Count == 0) break;

            int randomIndex = Random.Range(0, opciones.Count);
            string mejora = opciones[randomIndex];
            opciones.RemoveAt(randomIndex);

            botonesMejoras[i].GetComponentInChildren<TMP_Text>().text = mejora;

            botonesMejoras[i].onClick.RemoveAllListeners();
            botonesMejoras[i].onClick.AddListener(() => SeleccionarMejora(mejora));
        }
    }

    // Cuando el jugador selecciona una mejora
    void SeleccionarMejora(string mejora)
    {
        Debug.Log($"Mejora seleccionada: {mejora}");

        // Aplicar efectos según mejora
        switch (mejora)
        {
            case "Salud":
                playerHealth.AumentarSalud(20f);
                break;
            case "Velocidad":
                playerHealth.GetComponent<PlayerController>().moveSpeed += 2f;
                break;
            case "Empuje":
                playerHealth.AumentarEmpuje(1.2f);
                break;
            case "Daño":
                playerHealth.AumentarDaño(1.2f);
                break;
            case "Daño de Empuje":
                playerHealth.AumentarDañoEmpuje(5f);
                break;
        }

        // Cerrar menú y empezar siguiente ronda
        mejorasUI.SetActive(false);
        Time.timeScale = 1f;

        rondaActual++;
        IniciarRonda();
    }

    // Llamado desde EnemyHealth al morir
    public void EnemigoDerrotado()
    {
        if (juegoTerminado || !rondaActiva) return;

        enemigosRestantes--;
        ActualizarEnemigosUI();
        if (enemigosRestantes <= 0)
        {
            AcabarRonda();
        }
    }

    public void RegistrarEnemigo(GameObject enemigo)
    {
        if (juegoTerminado) return;
        enemigosRestantes++;
        ActualizarEnemigosUI();
    }

    public void DesregistrarEnemigo(GameObject enemigo)
    {
        if (juegoTerminado) return;

        enemigosRestantes--;
        ActualizarEnemigosUI();
        if (enemigosRestantes <= 0 && rondaActiva)
        {
            AcabarRonda();
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
        rondaActiva = false;
        temporizadorActivo = false;
        Time.timeScale = 0f;

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            gameOverText.text = mensaje;
        }

        var cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null)
            cursorManager.DesactivarCrosshair();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Desactiva el movimiento del jugador
        if (playerHealth != null)
        {
            var controller = playerHealth.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;
        }
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); //Vuelve al menú principal
    }
}
