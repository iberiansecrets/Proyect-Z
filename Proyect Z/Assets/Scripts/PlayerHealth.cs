using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida del jugador")]
    public float vidaMaxima = 100f;
    private float vidaActual;

    [Header("Da�o y cooldown")]
    public float cooldownDa�o = 1f;
    private float tiempoUltimoDa�o = -999f;

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

    public void RecibirDa�o(float cantidad)
    {
        if (Time.time - tiempoUltimoDa�o < cooldownDa�o)
            return; // A�n en cooldown

        tiempoUltimoDa�o = Time.time;
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
        // Aqu� podr�as desactivar controles, reproducir animaciones, etc.
    }
}
