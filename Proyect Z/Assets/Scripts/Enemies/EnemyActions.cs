using BehaviourAPI.Core;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using BehaviourAPI.UtilitySystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Contiene únicamente acciones y condiciones
/// para ser enlazadas desde la FSM (Moore)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SightSensor))]
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

    private Vector3 roamDirection;
    private float roamTimer;
    private bool isRoamingMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sightSensor = GetComponent<SightSensor>();
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
    /// Acción OnEnter del estado Roaming
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
    }

    /// <summary>
    /// Acción Tick del estado Roaming
    /// </summary>
    public Status TickRoaming()
    {
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

        // PERCEPCIÓN AQUÍ
        if (target != null && sightSensor.CanSeeTarget(target))
            return Status.Success;   // transición a Chasing

        return Status.Running;
    }


    // ======================================================
    // ===================== CHASING ========================
    // ======================================================

    /// <summary>
    /// Acción Tick del estado Chasing
    /// </summary>
    public Status TickChasing()
    {
        if (target == null)
            return Status.Failure;

        if (!sightSensor.CanSeeTarget(target))
            return Status.Failure;   // transición a Roaming

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
    /// Condición FSM: ¿el zombie ve al jugador?
    /// </summary>
    public Status CanSeePlayer()
    {
        if (target == null || sightSensor == null)
            return Status.Running;

        return sightSensor.CanSeeTarget(target)
            ? Status.Success
            : Status.Running;
    }

    public Status LostPlayer()
    {
        if (target == null || sightSensor == null)
            return Status.Success; // no target = perdido

        return !sightSensor.CanSeeTarget(target)
            ? Status.Success
            : Status.Running;
    }

}
