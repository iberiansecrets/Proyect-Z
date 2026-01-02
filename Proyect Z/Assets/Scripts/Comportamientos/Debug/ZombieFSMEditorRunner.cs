using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;

/// <summary>
/// Runner editable (plantilla ampliada) para el enemigo.
/// Mantiene la estructura original del template y añade:
/// - cacheo de VisionSensor / HearingSensor
/// - auto-asignación de target (Player tag)
/// - métodos booleanos para usar en Custom Perceptions desde el editor
/// - helper para obtener la última posición del sonido
/// - sitio para modificar grafos por código en ModifyGraphs
/// - aliases Check* para facilitar selección en el editor (estilo "chicken demo")
/// </summary>
[DisallowMultipleComponent]
public class ZombieFSMEditorRunner : EditorBehaviourRunner
{
    [Header("Target (opcional)")]
    public Transform target;

    [Header("Debug")]
    public bool debugLogs = true;

    // Sensores cacheados
    private VisionSensor visionSensor;
    private HearingSensor hearingSensor;

    #region ----------------------- ModifyGraphs (opcional) -----------------------

    // Use this method to modify the editor graph in code (se ejecuta cuando el editor carga/serializa)
    // graphMap contiene todos los grafos cargados, pushPerceptionMap contiene percepciones "push" definidas en el editor.
    protected override void ModifyGraphs(Dictionary<string, BehaviourGraph> graphMap, Dictionary<string, PushPerception> pushPerceptionMap)
    {
        // Ejemplos comentados de uso:
        // if (graphMap.TryGetValue("Main Graph", out BehaviourGraph graph))
        // {
        //     var node = graph.FindNode<BehaviourAPI.Core.UtilitySystem.UtilityNode>("chooseAction");
        //     if (node != null) { /* modificar node si es necesario */ }
        // }

        // if (pushPerceptionMap.TryGetValue("heard_sound", out PushPerception push))
        // {
        //     // personalizar push (si tu flujo lo requiere)
        // }

        // No realizar cambios por defecto.
    }

    #endregion

    #region ----------------------- Ciclo de vida (plantilla) -----------------------

    // Use this method instead of Awake
    protected override void Init()
    {
        // Cacheamos sensores del mismo GameObject (si no existen, intentar buscarlos)
        visionSensor = GetComponent<VisionSensor>();
        hearingSensor = GetComponent<HearingSensor>();

        // Auto-assign target si no se asignó en inspector
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (debugLogs)
        {
            Debug.Log($"[ZombieRunner Init] vision={(visionSensor != null)}, hearing={(hearingSensor != null)}, target={(target != null ? target.name : "NULL")}");
        }

        // Mantener la llamada a la implementación base (plantilla)
        base.Init();
    }

    // Use this method instead of OnDisable
    protected override void OnDisableSystem()
    {
        base.OnDisableSystem();
    }

    // Use this method instead of OnEnable
    protected override void OnEnableSystem()
    {
        base.OnEnableSystem();
    }

    // Use this method instead of Start
    protected override void OnStarted()
    {
        if (debugLogs) Debug.Log("[ZombieRunner] OnStarted");
        base.OnStarted();
    }

    // Use this method instead of Update
    protected override void OnUpdated()
    {
        base.OnUpdated();

        // --- DEBUG TEMPORAL: invoca los checks manualmente 1 vez por segundo ---
        // (esto es solo para diagnostico -- quitad despues)
        if (Time.frameCount % 60 == 0) // aprox cada 1s si 60fps
        {
            bool sees = CheckCanSeePlayer();
            bool heard = CheckHasHeardSound();
            Debug.Log($"[DEBUG RUNNER CHECKS] sees={sees} heard={heard}");
        }
    }


    #endregion

    #region ----------------------- Percepciones / Helpers -----------------------

    // MÉTODOS booleanos listos para usarse desde "Custom Perception -> Check" en transiciones.
    // Estos devuelven true/false (el editor mapeará true -> Success y false -> Failure)

    /// <summary>
    /// Método booleano (alias) para chequear si el player está visible desde el sensor de visión.
    /// Mantengo Runner_CanSeePlayer y CheckCanSeePlayer por compatibilidad.
    /// </summary>
    public bool Runner_CanSeePlayer()
    {
        return Internal_CanSeePlayer();
    }

    /// <summary>
    /// Alias con nombre tipo 'Check' (útil para el inspector que busca métodos llamados 'Check*')
    /// </summary>
    public bool CheckCanSeePlayer()
    {
        return Internal_CanSeePlayer();
    }

    private bool Internal_CanSeePlayer()
    {
        // intentar cachear sensores si son null (por si se añadieron después)
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (visionSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no visionSensor)");
            return false;
        }

        if (target == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no target)");
            return false;
        }

        bool sees = visionSensor.CanSeePlayerSimple(target);
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_CanSeePlayer = {sees}");
        return sees;
    }

    /// <summary>
    /// Método booleano para chequear si el hearing sensor tiene memoria de un sonido reciente.
    /// </summary>
    public bool Runner_HasHeardSound()
    {
        return Internal_HasHeardSound();
    }

    /// <summary>
    /// Alias Check...
    /// </summary>
    public bool CheckHasHeardSound()
    {
        return Internal_HasHeardSound();
    }

    private bool Internal_HasHeardSound()
    {
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
        if (hearingSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_HasHeardSound = false (no hearingSensor)");
            return false;
        }

        bool h = hearingSensor.HasHeardSound();
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_HasHeardSound = {h}");
        return h;
    }

    /// <summary>
    /// Devuelve la última posición escuchada (helper para acciones que la necesiten).
    /// </summary>
    public Vector3 Runner_GetLastHeardPosition()
    {
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
        if (hearingSensor == null) return transform.position;
        if (hearingSensor.TryGetLastHeardPosition(out Vector3 pos)) return pos;
        return transform.position;
    }

    /// <summary>
    /// Método de debug que siempre devuelve true (útil para testar bindings en transiciones).
    /// Recuerda quitarlo una vez depurado.
    /// </summary>
    public bool AlwaysTrue_Debug()
    {
        if (debugLogs) Debug.Log("[ZombieRunner] AlwaysTrue_Debug called");
        return true;
    }

    /// <summary>
    /// Permite que otros scripts (por ejemplo ZombieActions) asignen el target al runner en runtime.
    /// </summary>
    public void SetTarget(Transform t)
    {
        target = t;
    }

    #endregion

    #region ----------------------- Editor helper -----------------------

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Facilita que al editar el prefab en el editor se cacheen referencias visibles.
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
    }
#endif

    #endregion
}
