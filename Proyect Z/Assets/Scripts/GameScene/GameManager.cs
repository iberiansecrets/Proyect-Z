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

    [Header("Temporizador")]
    public float tiempoTotal = 600f; // 10 minutos
    private bool temporizadorActivo = true;

    [Header("Inactividad")]
    public float maxTiempoSinMatar = 30f; // Tiempo límite
    private float timerSinMatar = 0f;      // Contador interno

    [Header("Mejoras")]
    public GameObject mejorasUI; // Panel con los botones de mejoras
    public Button[] botonesMejoras; // Array de 3 botones para las mejoras
    private string[] mejoras = new string[]
    {
        "Salud",
        "Velocidad",
        "Empuje",
        "Impacto",
        "Impacto de Empuje"
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (mejorasUI != null)
            mejorasUI.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(VolverAlMenu);

        IniciarRonda();
    }

    void Update()
    {
        if (temporizadorActivo && !juegoTerminado)
        {
            tiempoTotal -= Time.deltaTime;
            ActualizarTimerUI();

            if (tiempoTotal <= 0)
            {
                tiempoTotal = 0;
                temporizadorActivo = false;
                FinalizarJuego("Se acabo el tiempo!");
            }
        }

        // Lógica de inactividad
        if (rondaActiva && !juegoTerminado)
        {
            timerSinMatar += Time.deltaTime;
        }
    }

    public bool MuchoSinMatar()
    {
        return timerSinMatar > maxTiempoSinMatar;
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
            enemigosRestantes = enemiesSpawner.zombiesSpawned.Count;
            // Quitamos el "/2" para que la UI no invente datos erróneos
            enemigosRestantesText.text = $"Zombies restantes: {enemigosRestantes}";
        }
    }

    void IniciarRonda()
    {
        Debug.Log($"Iniciando ronda {rondaActual}");

        // Calcula la cantidad de enemigos base de la ronda
        enemigosRestantes = enemigosPorRonda;

        ActualizarEnemigosUI();

        if (rondaText != null)
            rondaText.text = $"Ronda: {rondaActual}";

        // Genera los enemigos de la oleada (El Comandante restará 1 de forma interna en su rutina)
        if (enemiesSpawner != null)
            enemiesSpawner.GenerarOleada(enemigosRestantes, rondaActual);

        rondaActiva = true;
        juegoTerminado = false;
        temporizadorActivo = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        var cursorMgr = FindObjectOfType<CursorManager>();
        if (cursorMgr != null) cursorMgr.ActivarCrosshair();
    }

    void AcabarRonda()
    {
        Debug.Log($"Ronda {rondaActual} acabada!");
        rondaActiva = false;
        Time.timeScale = 0f;
        temporizadorActivo = false;

        float random = Random.Range(3, 5 * dificultad);
        enemigosPorRonda += (int)random;

        var cursorMgr = FindObjectOfType<CursorManager>();
        if (cursorMgr != null) cursorMgr.DesactivarCrosshair();

        if (rondaActual >= maxRondas)
        {
            FinalizarJuego("Has ganado!");
            return;
        }

        if (mejorasUI != null)
        {
            mejorasUI.SetActive(true);
            OpcionesMejoras();
        }
    }

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

    void SeleccionarMejora(string mejora)
    {
        Debug.Log($"Mejora seleccionada: {mejora}");

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
            case "Impacto":
                playerHealth.AumentarDaño(1.2f);
                break;
            case "Impacto de Empuje":
                playerHealth.AumentarDañoEmpuje(5f);
                break;
        }

        mejorasUI.SetActive(false);
        Time.timeScale = 1f;

        rondaActual++;
        IniciarRonda();
    }

    // Llamado desde EnemySpawner u OnZombieDeath al morir un zombie
    public void EnemigoDerrotado()
    {
        if (juegoTerminado || !rondaActiva) return;

        timerSinMatar = 0f; // Resetea la inactividad

        enemigosRestantes--;
        ActualizarEnemigosUI();

        if (enemigosRestantes <= 0)
        {
            AcabarRonda();
        }
    }

    public void RegistrarEnemigo(GameObject enemigo, bool esInvocado = false)
    {
        if (juegoTerminado) return;
                
        if (esInvocado)
        {
            enemigosRestantes++;
        }

        ActualizarEnemigosUI();
    }

    public void DesregistrarEnemigo(GameObject enemigo)
    {
        if (juegoTerminado) return;
        
        ActualizarEnemigosUI();
        if (enemigosRestantes <= 0 && rondaActiva)
        {
            AcabarRonda();
        }
    }

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

        var vialChanger = FindObjectOfType<VialChanger>();
        if (vialChanger != null)
        {
            bool victoria = mensaje == "Has ganado!";
            vialChanger.MostrarResultado(victoria);
        }

        var cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null)
            cursorManager.DesactivarCrosshair();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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
        SceneManager.LoadScene("MainMenu");
    }
}