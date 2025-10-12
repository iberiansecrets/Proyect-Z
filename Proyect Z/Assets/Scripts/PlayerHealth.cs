using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida del jugador")]
    public float vidaMaxima = 100f;
    private float vidaActual;

    [Header("Daño y cooldown")]
    public float cooldownDaño = 1f;
    private float tiempoUltimoDaño = -999f;

    [Header("UI")]
    public Slider barraDeVida;

    void Start()
    {
        vidaActual = vidaMaxima;

        if (barraDeVida != null)
            barraDeVida.maxValue = vidaMaxima;
    }

    void Update()
    {
        if (barraDeVida != null)
            barraDeVida.value = vidaActual;
    }

    public void RecibirDaño(float cantidad)
    {
        if (Time.time - tiempoUltimoDaño < cooldownDaño)
            return; // Aún en cooldown

        tiempoUltimoDaño = Time.time;
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);

        if (vidaActual <= 0)
        {
            Muerte();
        }
    }

    private void Muerte()
    {
        Debug.Log("Jugador muerto");
        // Aquí podrías desactivar controles, reproducir animaciones, etc.
    }
}
