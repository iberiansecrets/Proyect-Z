using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UtilitySystems;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class ZComandanteBehaviour : BehaviourRunner
{
    [Header("Configuración de Rangos")]
    public float distanciaSegura = 15f;
    public float distanciaPanico = 5f;
    public float cooldownHabilidadMax = 8f;

    [Header("Atributos de Movimiento")]
    public float speedAvanzar = 3f;
    public float speedRetroceder = 2f;
    public float speedHuir = 5.5f;
    public float speedRotation = 250f;

    // Variables de control de estado interno
    private float distanciaAlJugador;
    private float cooldownHabilidadTimer = 0f;
    private float panicTimer = 0f;
    private float ejeVelocidadAnim = 0f;

    // Componentes y Referencias
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator zombiAnim;
    public Transform jugador;
    private EnemiesSpawner spawner;
    private float lastHealth;
    private EnemyHealth healthComponent;

    protected override void Init()
    {
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        zombiAnim = GetComponentInChildren<Animator>();
        spawner = FindAnyObjectByType<EnemiesSpawner>();
        healthComponent = GetComponent<EnemyHealth>();

        if (healthComponent != null) lastHealth = healthComponent.vidaMaxima;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        // Desactivamos la actualización automática para controlar la rotación de espaldas por código
        agent.updatePosition = false;
        agent.updateRotation = false;

        base.Init();


    }

    protected override void OnUpdated()
    {
        // Actualización continua de variables de entorno (Sensores)
        if (jugador != null)
        {
            distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        }

        if (cooldownHabilidadTimer > 0)
            cooldownHabilidadTimer -= Time.deltaTime;

        if (panicTimer > 0)
            panicTimer -= Time.deltaTime;

        // Detección reactiva de daño recibido
            if (healthComponent != null && healthComponent.GetVidaActual() < lastHealth) // Asumiendo lógica destructiva o decremento
        {
            Debug.Log("ME HAN DADO");
            panicTimer = 5f; // 2.5 segundos de pánico máximo
            
            lastHealth = healthComponent.GetVidaActual();

            Debug.Log("paniTimerr = " + panicTimer + " ; VIDA Actual: " + lastHealth);
        }


        // Suavizado del parámetro del Animator para transiciones orgánicas
        zombiAnim.SetFloat("VelocidadEje", ejeVelocidadAnim);

        if (agent.hasPath)
        {
            Debug.Log($"Agente moviéndose hacia: {agent.destination} | Distancia: {distanciaAlJugador}");
        }

        base.OnUpdated();
    }

    // Creación del Grafo del Sistema de Utilidad
    
    protected override BehaviourGraph CreateGraph()
    {   
        // Se inicializa el sistema con la inercia por defecto (1.3f)
        UtilitySystem us = new UtilitySystem(1.3f);

        // DEFINICIÓN DE FACTORES DE ENTORNO (Usando CreateVariable)
        VariableFactor fDistancia = us.CreateVariable(() => distanciaAlJugador, 0f, 25f);
        VariableFactor fCooldown = us.CreateVariable(() => cooldownHabilidadTimer <= 0 ? 1f : 0f, 0f, 1f);
        VariableFactor fPanico = us.CreateVariable(() => Mathf.Clamp01(panicTimer / 2.5f), 0f, 1f);

        Debug.Log("Factores creados. Distancia actual: " + distanciaAlJugador);

        // APLICACIÓN DE CURVAS

        // Huir
        CustomCurveFactor curveHuir = us.CreateCurve<CustomCurveFactor>(fDistancia);
        curveHuir.Function = (d) => Mathf.Max(fPanico.Utility, 1f - d); 

        // Retroceder:
        CustomCurveFactor curveRetroceder = us.CreateCurve<CustomCurveFactor>(fDistancia);
        curveRetroceder.Function = (d) => Mathf.Exp(-Mathf.Pow(d - 0.3f, 2) / 0.05f);

        // Avanzar:
        LinearCurveFactor curveAvanzar = us.CreateCurve<LinearCurveFactor>(fDistancia);
        curveAvanzar.Slope = 1f; 
        curveAvanzar.YIntercept = 0f;
        //curveRetroceder.Function = (d) => 1.0f;

        // Ordenar: 
        WeightedFusionFactor factorOrdenar = us.CreateFusion<WeightedFusionFactor>(fCooldown, fDistancia);
        factorOrdenar.Weights = new float[] { 0.7f, 0.3f };

        // Burla:
        VariableFactor factorBurla = us.CreateVariable(() => (cooldownHabilidadTimer > cooldownHabilidadMax - 1.5f) ? 1.0f : 0.0f, 0f, 1f);

        // ACCIONES
        us.CreateAction(curveHuir, new FunctionalAction(EjecutarHuir));
        us.CreateAction(curveRetroceder, new FunctionalAction(EjecutarRetroceder));
        us.CreateAction(curveAvanzar, new FunctionalAction(EjecutarAvanzar));
        us.CreateAction(factorOrdenar, new FunctionalAction(EjecutarOrden));
        us.CreateAction(factorBurla, new FunctionalAction(EjecutarBurla));

        var debugger = GetComponent<BSRuntimeDebugger>();
        if (debugger != null)
        {
            debugger.RegisterGraph(us, "ZComandanteUS");
        }

        return us;
    }

    
    public Status EjecutarAvanzar()
    {
        Debug.Log("DEBO AVANZAR");
        ejeVelocidadAnim = 1.0f; // Animación Avanzar frontal
        agent.speed = speedAvanzar;
        agent.SetDestination(jugador.position);
        
        if (agent.pathPending == false)
        {
            MoverAgenteHaciaDestino(false);
        }
        else
        {
            MoverAgenteHaciaDestino(true);
        }

        return Status.Running;
    }

    public Status EjecutarRetroceder()
    {
        Debug.Log("RETROCEDO");
        ejeVelocidadAnim = -1.0f; // Animación Retroceder de espaldas
        agent.speed = speedRetroceder;

        // Calculamos una posición hacia atrás respecto a la línea con el jugador
        Vector3 dirDesdeJugador = (transform.position - jugador.position).normalized;
        Vector3 destinoRetroceso = transform.position + dirDesdeJugador * 3f;
        agent.SetDestination(destinoRetroceso);

        MoverAgenteHaciaDestino(true); // Forzar LookAt hacia el jugador
        return Status.Running;
    }

    public Status EjecutarHuir()
    {
        Debug.Log("HUYO");
        ejeVelocidadAnim = 2.0f; // Animación de Huir rápido de espaldas (requisito cobarde)
        agent.speed = speedHuir;

        Vector3 dirDesdeJugador = (transform.position - jugador.position).normalized;
        Vector3 destinoHuida = transform.position + dirDesdeJugador * 6f;
        agent.SetDestination(destinoHuida);

        MoverAgenteHaciaDestino(false); // Mantiene al jugador vigilado mientras corre
        return Status.Running;
    }

    public Status EjecutarOrden()
    {
        Debug.Log("MANDO ZOMBIS");
        agent.ResetPath();
        zombiAnim.SetTrigger("Ordenar"); // Animación Señalar hacia delante
        ejeVelocidadAnim = 0f; // Idle de animación de risa

        // Calcular dirección al jugador
        Vector3 dirAlJugador = (jugador.position - transform.position).normalized;
        dirAlJugador.y = 0; // Mantener solo en plano horizontal

        // Rotación suave hacia el jugador
        if (dirAlJugador != Vector3.zero)
        {
            Quaternion rotObjetivo = Quaternion.LookRotation(dirAlJugador);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, speedRotation * 2 * Time.deltaTime);
        }

        // Invocación a través del Spawner del nivel
        if (spawner != null)
        {
            // Genera una pequeña oleada de esbirros protectores (ej: 4 zombis normales, ronda actual)
            spawner.GenerarOleada(4, 1);
        }

        // Activamos el cooldown inmediatamente para ceder el peso a la Burla/Risa
        cooldownHabilidadTimer = cooldownHabilidadMax;
        return Status.Success;
    }

    public Status EjecutarBurla()
    {
        Debug.Log("ME BURLO");
        agent.ResetPath();
        zombiAnim.SetBool("Burlarse", true);
        ejeVelocidadAnim = 0f; // Idle de animación de risa

        // Permanecerá en este estado mientras el factorBurla devuelva utilidad máxima (1.5s)
        return Status.Running;
    }

    // Apagado de banderas específicas al salir de nodos de utilidad mediante interrupción
    protected void OnDisable()
    {
        if (zombiAnim != null) zombiAnim.SetBool("Burlarse", false);
    }

    
    // SUB-SISTEMA DE NAVEGACIÓN Y ORIENTACIÓN MANUAL
    private void MoverAgenteHaciaDestino(bool lookAtPlayer)
    {
        Debug.Log("ME MUEVO");
        zombiAnim.SetBool("Burlarse", false);

        Debug.Log("PAthPending = " + agent.pathPending);
        if (agent.pathPending) return;

        // Sincronización del NavMeshAgent con el Rigidbody manual para evitar desfases de colisión
        Vector3 direccionCamino = (agent.steeringTarget - rb.position).normalized;
        direccionCamino.y = 0;

        if (Vector3.Distance(transform.position, agent.destination) < 0.5f) return;

        // Gestión Avanzada del Forward (Eje de visión del Zombie)
        if (lookAtPlayer && jugador != null)
        {
            Vector3 dirHaciaJugador = (jugador.position - rb.position).normalized;
            dirHaciaJugador.y = 0;
            if (dirHaciaJugador.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirHaciaJugador);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, speedRotation * Time.deltaTime));
            }
        }
        else if (direccionCamino.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direccionCamino);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, speedRotation * Time.deltaTime));
        }

        // Desplazamiento por física usando el vector calculado por el NavMesh
        if (direccionCamino.sqrMagnitude > 0.01f)
        {
            rb.MovePosition(rb.position + direccionCamino * agent.speed * Time.deltaTime);
        }

        agent.nextPosition = transform.position; // Forzar al agente NavMesh a seguir al Rigidbody
    }
}