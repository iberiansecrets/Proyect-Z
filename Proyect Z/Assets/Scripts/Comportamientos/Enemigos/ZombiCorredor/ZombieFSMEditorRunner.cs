using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;

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

    #region ----------------------- Ciclo de vida (plantilla) -----------------------

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

    protected override void OnDisableSystem()
    {
        base.OnDisableSystem();
    }

    protected override void OnEnableSystem()
    {
        base.OnEnableSystem();
    }

    protected override void OnStarted()
    {
        if (debugLogs) Debug.Log("[ZombieRunner] OnStarted");
        base.OnStarted();
    }

    protected override void OnUpdated()
    {
        base.OnUpdated();

        if (Time.frameCount % 60 == 0)
        {
            bool sees = CheckCanSeePlayer();
            bool heard = CheckHasHeardSound();
            Debug.Log($"[DEBUG RUNNER CHECKS] sees={sees} heard={heard}");
        }
    }


    #endregion

    #region ----------------------- Percepciones / Helpers -----------------------

    // METODOS booleanos listos para usarse desde "Custom Perception -> Check" en transiciones.
    // Estos devuelven true/false (el editor mapeará true -> Success y false -> Failure)

    /// Método booleano (alias) para chequear si el player está visible desde el sensor de visión.
    public bool Runner_CanSeePlayer()
    {
        return Internal_CanSeePlayer();
    }

    /// Alias con nombre tipo 'Check'
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

    /// Método booleano para chequear si el hearing sensor tiene memoria de un sonido reciente.
    public bool Runner_HasHeardSound()
    {
        return Internal_HasHeardSound();
    }

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

    /// Devuelve la última posición escuchada (helper para acciones que la necesiten).
    public Vector3 Runner_GetLastHeardPosition()
    {
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
        if (hearingSensor == null) return transform.position;
        if (hearingSensor.TryGetLastHeardPosition(out Vector3 pos)) return pos;
        return transform.position;
    }

    /// Permite que otros scripts (por ejemplo ZombieActions) asignen el target al runner en runtime.
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
