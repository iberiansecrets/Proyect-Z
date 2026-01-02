using BehaviourAPI.Core;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using BehaviourAPI.UtilitySystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Contiene unicamente acciones y condiciones
/// para ser enlazadas desde la FSM (Moore)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SightSensor))]
[RequireComponent(typeof(SoundSensor))]
public class EnemyActions : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Roaming")]
    public float roamSpeed = 2f;
    public float roamMoveTime = 2f;
    public float roamWaitTime = 5f;

    [Header("Chasing")]
    public float chaseSpeed = 3f;

    // --- Internal state ---
    private Rigidbody rb;
    private SightSensor sightSensor;
    private SoundSensor soundSensor;
    private Vector3 investigateTarget;

    private Vector3 roamDirection;
    private float roamTimer;
    private bool isRoamingMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sightSensor = GetComponent<SightSensor>();
        soundSensor = GetComponent<SoundSensor>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[EnemyActions] Player not found by tag");
            }
        }
    }

    // ======================================================
    // ===================== ROAMING ========================
    // ======================================================

    /// <summary>
    /// Accion OnEnter del estado Roaming
    /// </summary>
    public void EnterRoaming()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        roamDirection = new Vector3(randomDir.x, 0f, randomDir.y);

        isRoamingMoving = true;
        roamTimer = roamMoveTime;

        if (roamDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(roamDirection));
        }

        Debug.Log("[EnemyActions] EnterRoaming");
    }

    /// <summary>
    /// Accion Tick del estado Roaming
    /// </summary>
    public Status TickRoaming()
    {
        Debug.Log("[EnemyActions] TickRoaming ejecutandose");
        roamTimer -= Time.deltaTime;

        if (isRoamingMoving)
        {
            rb.MovePosition(rb.position + roamDirection * roamSpeed * Time.deltaTime);

            if (roamTimer <= 0f)
            {
                isRoamingMoving = false;
                roamTimer = roamWaitTime;
            }
        }
        else
        {
            if (roamTimer <= 0f)
                EnterRoaming();
        }

        // NOTA: no devolvemos Success desde aquí por visión/sonido.
        // Las transiciones deben manejarse por percepciones en el Runner.
        return Status.Running;
    }


    // ======================================================
    // ===================== CHASING ========================
    // ======================================================

    /// <summary>
    /// Accion Tick del estado Chasing
    /// </summary>
    public Status TickChasing()
    {
        if (target == null)
            return Status.Failure;

        if (!sightSensor.CanSeeTarget(target))
            return Status.Failure;   // transicion a Roaming

        Vector3 dir = (target.position - transform.position).normalized;

        rb.MovePosition(rb.position + dir * chaseSpeed * Time.deltaTime);

        if (dir != Vector3.zero)
            rb.MoveRotation(Quaternion.LookRotation(dir));

        return Status.Running;
    }


    // ======================================================
    // ==================== CONDITIONS ======================
    // ======================================================

    /// <summary>
    /// Condicion FSM: el zombie ve al jugador?
    /// (Si quieres usar esto como "Condition node" en el editor, devuelve Success/Failure)
    /// </summary>
    public Status CanSeePlayer()
    {
        if (target == null || sightSensor == null)
            return Status.Failure; // <<< Cambiado a Failure para uso como Condition

        return sightSensor.CanSeeTarget(target)
            ? Status.Success
            : Status.Failure;
    }

    public Status LostPlayer()
    {
        if (target == null || sightSensor == null)
            return Status.Success; // no target = perdido

        return !sightSensor.CanSeeTarget(target)
            ? Status.Success
            : Status.Failure;
    }

    public Status HasHeardSoundStatus()
    {
        // <<< Usar el sensor cacheado en vez de GetComponent
        return soundSensor != null && soundSensor.HasHeardSound()
            ? Status.Success
            : Status.Failure;
    }

    public void EnterInvestigateSound()
    {
        if (soundSensor == null || !soundSensor.HasHeardSound())
            return;

        investigateTarget = soundSensor.GetLastHeardPosition();

        // miramos hacia el objetivo y loggeamos
        Vector3 dir = (investigateTarget - transform.position);
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(dir.normalized));
        }

        Debug.Log($"[EnemyActions] EnterInvestigateSound target={investigateTarget}");
    }

    public Status TickInvestigateSound()
    {
        if (!soundSensor.HasHeardSound())
            return Status.Failure; // ya no hay sonido -> volver a roaming

        Vector3 targetPos = soundSensor.GetLastHeardPosition();
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;

        if (dir.magnitude < 0.5f)
        {
            // He llegado y NO veo al jugador
            if (!sightSensor.CanSeeTarget(target))
                return Status.Success; // INVESTIGACION COMPLETADA
        }

        // <<< Unificamos movimiento con MovePosition/MoveRotation
        Vector3 move = dir.normalized * roamSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);
        if (dir != Vector3.zero)
            rb.MoveRotation(Quaternion.LookRotation(dir.normalized));

        return Status.Running;
    }
}
