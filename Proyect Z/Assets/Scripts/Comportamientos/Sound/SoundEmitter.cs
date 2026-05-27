using System.Collections.Generic;
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    [Header("Configuración de Sonidos")]
    public List<SoundData> sounds;

    // --- VARIABLES PARA EL GIZMO DINÁMICO ---
    private float debugGizmoRadius = 0f;
    private float debugGizmoTimer = 0f;
    private float debugGizmoDuration = 1.0f; // Tiempo que tarda en desaparecer la onda en la ventana Scene
    // ----------------------------------------

    public void EmitSound(SoundType type)
    {
        // 1. Buscamos el índice del sonido en la lista
        int dataIndex = sounds.FindIndex(s => s.type == type);

        // Si es -1, significa que no existe en el Inspector
        if (dataIndex == -1)
        {
            Debug.LogError($"<color=red>[SoundEmitter] ¡ERROR! No se encontró configuración para: {type}.</color>");
            return;
        }

        // Extraemos la información
        SoundData data = sounds[dataIndex];

        // --- ACTIVAR EL GIZMO ---
        debugGizmoRadius = data.radius;
        debugGizmoTimer = debugGizmoDuration;
        // ------------------------

        // 2. Lanzamos la esfera de sonido (Ignorando Triggers para evitar choques fantasma)
        Collider[] hits = Physics.OverlapSphere(transform.position, data.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // 3. Revisamos si alguno de esos objetos tiene oídos
        foreach (Collider hit in hits)
        {
            HearingSensor sensor = hit.GetComponent<HearingSensor>();
            if (sensor == null) sensor = hit.GetComponentInParent<HearingSensor>();

            if (sensor != null)
            {
                sensor.NotifySound(transform.position, data.intensity);
            }
        }
    }

    private void Update()
    {
        // Reducimos el temporizador del Gizmo frame a frame
        if (debugGizmoTimer > 0)
        {
            debugGizmoTimer -= Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        // Solo dibujamos la esfera de sonido si acabamos de disparar
        if (debugGizmoTimer > 0)
        {
            // Calculamos un valor de 0 a 1 para hacer un efecto de desvanecimiento (fade out)
            float alpha = debugGizmoTimer / debugGizmoDuration;

            // Dibujamos el contorno de la esfera (Wireframe)
            Gizmos.color = new Color(1f, 0f, 0f, alpha); // Rojo desvaneciéndose
            Gizmos.DrawWireSphere(transform.position, debugGizmoRadius);

            // Opcional: Dibujamos una esfera sólida muy transparente para que se vea mejor el volumen
            Gizmos.color = new Color(1f, 0f, 0f, alpha * 0.15f);
            Gizmos.DrawSphere(transform.position, debugGizmoRadius);
        }
    }
}