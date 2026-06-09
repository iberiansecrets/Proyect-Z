using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZComandanteNacimiento : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    // Referencia al script de IA que pasaste previamente para pausar su ejecución
    private ZComandanteBehaviour comportamiento;

    [Header("Configuración de Aparición")]
    public float profundidadEnterrado = 3f;
    public float tiempoElevacion = 2.5f;
    public float compensacionPivote = 1.5f; // Ajusta esto según el centro de tu modelo

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        comportamiento = GetComponent<ZComandanteBehaviour>();
    }

    void Start()
    {
        StartCoroutine(AparecerComoFantasma());
    }

    private IEnumerator AparecerComoFantasma()
    {
        // 1. Desactivamos el agente y la IA para evitar que fuercen movimientos o rotaciones
        if (agent != null) agent.enabled = false;
        if (comportamiento != null) comportamiento.enabled = false;

        // 2. Forzamos la animación Idle al entrar (velocidad 0)
        if (anim != null) anim.SetFloat("velocidadEje", 0f);

        // 3. Calculamos posiciones
        // Si el pivote está en el centro, usamos la compensacionPivote para elevarlo sobre el suelo real
        Vector3 posicionFinal = transform.position + (Vector3.up * compensacionPivote);
        Vector3 posicionInicial = posicionFinal + (Vector3.down * profundidadEnterrado);

        transform.position = posicionInicial;

        // 4. Elevación progresiva interpolada
        float tiempoPasado = 0f;
        while (tiempoPasado < tiempoElevacion)
        {
            tiempoPasado += Time.deltaTime;
            float t = tiempoPasado / tiempoElevacion;

            // Suavizado (Ease Out) para que frene al llegar arriba
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, t);
            yield return null;
        }

        // Aseguramos la posición exacta al terminar
        transform.position = posicionFinal;

        // 5. Devolvemos el control a los sistemas del zombi
        if (agent != null) agent.enabled = true;
        if (comportamiento != null) comportamiento.enabled = true;
    }
}