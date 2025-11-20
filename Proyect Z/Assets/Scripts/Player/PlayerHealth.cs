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

    [Header("Mejoras y atributos")]
    public float multiplicadorDaño = 1f;   // Mejora de daño general
    public float multiplicadorEmpuje = 1f; // Mejora de empuje
    public float dañoEmpuje = 0f;    // Empuje ofensivo

    [Header("UI")]
    public Slider barraDeVida;

    void Start()
    {
        vidaActual = 50;

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
            return; // Cooldown

        tiempoUltimoDaño = Time.time;
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);

        if (vidaActual <= 0)
        {
            Muerte();
        }
    }

    public void Heal(float cantidad)
    {
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
    }

    private void Muerte()
    {
        Debug.Log("Jugador muerto");
        GameManager.Instance.JugadorDerrotado();
    }

    public void AumentarSalud(float cantidad)
    {
        vidaMaxima += cantidad;
        vidaActual = vidaMaxima;
        if (barraDeVida != null)
        {
            barraDeVida.maxValue = vidaMaxima;
            barraDeVida.value = vidaActual;
        }
        Debug.Log("Salud aumentada en " + cantidad);
    }

    public void AumentarEmpuje(float factor)
    {
        multiplicadorEmpuje *= factor;
        Debug.Log("Empuje aumentado x" + factor);
    }

    public void AumentarDaño(float factor)
    {
        multiplicadorDaño *= factor;
        Debug.Log("Daño de armas aumentado x" + factor);
    }

    public void AumentarDañoEmpuje(float valor)
    {
        dañoEmpuje += valor;
        Debug.Log("El empuje ahora causa daño: " + valor);
    }

    public float GetVidaActual()
    {
        return vidaActual;
    }

    public float GetVidaMaxima()
    {
        return vidaMaxima;
    }
}
