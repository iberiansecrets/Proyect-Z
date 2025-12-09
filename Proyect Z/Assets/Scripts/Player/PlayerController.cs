using System.Collections;
using UnityEngine;
using Terresquall;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    [Header("Modelos de armas (hijos del weaponHolder)")]
    public GameObject pistolModel;
    public GameObject shotgunModel;
    public GameObject rifleModel;
    public GameObject sniperModel;
    
    [Header("Movimiento")]
    public float moveSpeed = 9f;

    [Header("Prefabs")]
    public GameObject pistolBulletPrefab;      // Bala normal (click izquierdo)
    public GameObject shotgunBulletPrefab;     // Balas de escopeta
    public GameObject rifleBulletPrefab;       // Balas de fusil de asalto
    public GameObject sniperBulletPrefab;      // Balas de francotirador
    public GameObject bulletPushPrefab;        // Bala que empuja (click derecho)
    public GameObject decoyPrefab;             // Señuelo

    [Header("Controles de móvil")]
    public GameObject moveJoystick; // Joystick de movimiento
    public GameObject shootJoystick; // Joystick de disparo
    public bool isMobile; // Comprobar si está en modo "Móvil"
    private Vector3 aimInput; // Dirección del joystick de disparo
    private float aimThreshold = 0.3f; // Sensibilidad para apuntar/disparar
    public GameObject dashButton; //Botón para dashear
    public GameObject shoveButton; // Botón para empujar
    public GameObject decoyButton; // Botón para señuelo

    public Transform bulletShot;               // Punto desde donde se dispara

    public GameObject tracerPrefab; // Prefab tracer escopeta
    private int pellets = 8;
    private float spreadAngle = 15f;
    private float range = 10f;
    private float damage = 10f;

    public float bulletSpeed = 100f;


    public float shotgunAngle = 10f;           // Grados de dispersión para izquierda y derecha
    public int shotgunPellets = 3;             // Balas que lanza la escopeta

    public float throwForce = 10f;             // Fuerza con la que se lanza el señuelo

    [Header("Delays")]
    public float shotgunFireDelay = 0.6f;      // Tiempo entre disparos de escopeta
    public float pistolFireDelay = 0.2f;       // Tiempo entre disparos de pistola
    public float pushFireDelay = 0.8f;         // Tiempo entre empujes
    public float rifleFireDelay = 0.15f;        // Tiempo entre disparos de balas de fusil
    public float sniperFireDelay = 1.2f;        // Tiempo entre disparos de francotirador

    private float nextFireTime = 0f;           // Control de cadencia de disparo del fusil

    private int numDecoy = 0; // Número de señuelos que tiene el jugador

    [Header("Temporizadores de armas")]
    private float shotgunTimer = 10f;
    private float rifleTimer = 8f;
    private float sniperTimer = 12f;

    private GameObject currentGunPrefab;       // Prefab del arma actual
    private Rigidbody rb;
    private Vector3 moveInput;
    private Animator anim;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pistolSFX;
    public AudioClip shotgunSFX;
    public AudioClip rifleSFX;
    public AudioClip sniperSFX;

    [Header("UI Armas")]
    public GameObject uiPistol;
    public GameObject uiShotgun;
    public GameObject uiRifle;
    public GameObject uiSniper;

    public TMPro.TMP_Text timerShotgunText;
    public TMPro.TMP_Text timerRifleText;
    public TMPro.TMP_Text timerSniperText;
    public TMP_Text decoyText;

    public bool isPaused = false;

    [Header("Empuje (Hitbox)")]
    public Vector3 shoveBoxSize = new Vector3(3f, 3f, 3f); // ancho, alto, largo
    public float shoveCooldown = 0.8f;
    public float shoveForce = 800f;
    public string enemigoTag = "Enemy";

    private float lastShoveTime;

    [Header("Esquive / Dash")]
    public float dashDistance = 6f;      // Distancia
    public float dashDuration = 0.15f;  // Duracion
    public float dashCooldown = 0.8f;   // Cooldown

    private bool isDashing = false;
    private float lastDashTime = -999f;

    public bool isInvulnerable = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>();
        currentGunPrefab = pistolBulletPrefab; // Empieza con pistola
        ActualizarSeñueloUI();
        isMobile = Application.isMobilePlatform;

        if (isMobile)
        {
            Debug.Log("Estamos en móvil");
            moveJoystick.SetActive(true);
            shootJoystick.SetActive(true);
            shoveButton.SetActive(true);
            dashButton.SetActive(true);
            decoyButton.SetActive(true);
        }
        else
        {
            Debug.Log("Estamos en PC");
            moveJoystick.SetActive(false);
            shootJoystick.SetActive(false);
            shoveButton.SetActive(false);
            dashButton.SetActive(false);
            decoyButton.SetActive(false);
        }
    }

    void Update()
    {
        if (isPaused) return;
        float moveX = 0;
        float moveZ = 0;

        // Si está en móvil, coge los dos joysticks para disparar
        if (isMobile)
        {
            moveX = VirtualJoystick.GetAxis("Horizontal", 0);
            moveZ = VirtualJoystick.GetAxis("Vertical", 0);

            float aimX = VirtualJoystick.GetAxis("Horizontal", 1);
            float aimZ = VirtualJoystick.GetAxis("Vertical", 1);

            aimInput = new Vector3(aimX, 0f, aimZ);
        }
        else  // Si está en PC, usa los controles de siempre
        {
            moveX = Input.GetAxis("Horizontal");
            moveZ = Input.GetAxis("Vertical");
        }

        Vector3 moveWorld = new Vector3(moveX, 0f, moveZ).normalized;
        moveInput = moveWorld; // para usarlo en FixedUpdate para mover al personaje

        // Transformar al espacio local del personaje para las animaciones
        Vector3 moveLocal = transform.InverseTransformDirection(moveWorld);

        anim.SetFloat("MoveX", moveLocal.x);
        anim.SetFloat("MoveZ", moveLocal.z);
        anim.SetBool("IsMoving", moveLocal.magnitude > 0.1f);

        // Cambiar tipo de arma según el prefab actual
        if (currentGunPrefab == pistolBulletPrefab)
            anim.SetInteger("WeaponType", 0);
        else
            anim.SetInteger("WeaponType", 1);

        // Patada al pulsar click derecho o la tecla que quieras
        if (Input.GetButtonDown("Fire2"))
        {
            anim.SetTrigger("Kick");
        }

        // Rota los disparos y al jugador a donde apunta el joystick derecho
        if (isMobile && aimInput.magnitude > aimThreshold)
        {
            Vector3 aimDirection = aimInput.normalized;
            transform.rotation = Quaternion.LookRotation(aimDirection);
            bulletShot.rotation = Quaternion.LookRotation(aimDirection);
        }

        // Disparo principal
        if (isMobile)
        {
            ComprobarArmaMovil();
        }
        else
        {
            ComprobarArma();
        }

        // Click derecho: Empuje de zombies
        if (Input.GetButtonDown("Fire2")) {
            TryShove();
        }

        // Lanzamiento de señuelo
        if (Input.GetKeyDown(KeyCode.E) && numDecoy > 0)
        {
            LanzarDecoy();
        }

        // Deslizamiento
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDash();
        }
    }

    void FixedUpdate()
    {
        if (isPaused) return;
        if (isDashing) return;

        if (moveInput.magnitude > 0f)
        {
            Vector3 movimiento = moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movimiento);
        }
    }

    void ComprobarArmaMovil()
    {
        // Si el joystick no apunta, no dispares
        if (aimInput.magnitude < aimThreshold)
            return;

        // Si no toca disparar aún, no dispares
        if (Time.time < nextFireTime)
            return;

        // Determinar delay según arma
        float delay = GetCurrentFireDelay();
        nextFireTime = Time.time + delay;

        // Disparo según arma equipada
        if (currentGunPrefab == shotgunBulletPrefab)
        {
            ShootShotgun();
            PlayWeaponSound(shotgunSFX);
        }
        else
        {
            Shoot(currentGunPrefab);

            if (currentGunPrefab == pistolBulletPrefab) PlayWeaponSound(pistolSFX);
            if (currentGunPrefab == rifleBulletPrefab) PlayWeaponSound(rifleSFX);
            if (currentGunPrefab == sniperBulletPrefab) PlayWeaponSound(sniperSFX);
        }
    }

    public void ButtonDash()
    {
        TryDash();
    }

    public void ButtonShove()
    {
        TryShove();
    }

    public void ButtonDecoy()
    {
        if(numDecoy > 0)
        {
            LanzarDecoy();
        }
    }

    float GetCurrentFireDelay()
    {
        if (currentGunPrefab == rifleBulletPrefab) return rifleFireDelay;
        if (currentGunPrefab == shotgunBulletPrefab) return shotgunFireDelay;
        if (currentGunPrefab == sniperBulletPrefab) return sniperFireDelay;

        return pistolFireDelay; // Pistola por defecto
    }

    void ComprobarArma()
    {
        // Rifle automático
        if (currentGunPrefab == rifleBulletPrefab)
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + rifleFireDelay;
                Shoot(currentGunPrefab);
                PlayWeaponSound(rifleSFX);
            }
        }
        // Escopeta con retardo
        else if (currentGunPrefab == shotgunBulletPrefab)
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + shotgunFireDelay;
                ShootShotgun();
                PlayWeaponSound(shotgunSFX);
            }
        }
        // Francotirador con retardo
        else if (currentGunPrefab == sniperBulletPrefab)
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + sniperFireDelay;
                Shoot(currentGunPrefab);
                PlayWeaponSound(sniperSFX);
            }
        }
        // Pistola semiautomática con retardo
        else if (currentGunPrefab == pistolBulletPrefab)
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + pistolFireDelay;
                Shoot(currentGunPrefab);
                PlayWeaponSound(pistolSFX);
            }
        }
    }

    /*void Shoot(GameObject prefab)
    {
        if (prefab == null || bulletShot == null)
        {
            Debug.LogWarning("Prefab o bulletShot no asignado en PlayerController.");
            return;
        }

        GameObject bala = Instantiate(prefab, bulletShot.position, bulletShot.rotation);

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala != null)
        {
            rbBala.linearVelocity = bulletShot.forward * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("El prefab de bala no tiene Rigidbody.");
        }
    }*/

    void Shoot(GameObject prefab)
    {
        if (prefab == null || bulletShot == null)
        {
            Debug.LogWarning("Prefab o bulletShot no asignado en PlayerController.");
            return;
        }

        // Determinación spread
        float spreadAngle = 0f;
        if (prefab == pistolBulletPrefab)
            spreadAngle = 1.5f;
        else if (prefab == rifleBulletPrefab)
            spreadAngle = 3f;

        // Calcular rotación aleatoria dentro del spread
        Quaternion spreadRotation = bulletShot.rotation *
            Quaternion.Euler(Random.Range(-spreadAngle, spreadAngle), Random.Range(-spreadAngle, spreadAngle), 0f);

        // Instanciar la bala con esa rotación
        GameObject bala = Instantiate(prefab, bulletShot.position, spreadRotation);

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala != null)
        {
            rbBala.linearVelocity = bala.transform.forward * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("El prefab de bala no tiene Rigidbody.");
        }
    }

    /*void ShootShotgun()
    {
        float[] angulos = { -shotgunAngle, 0, shotgunAngle };

        foreach (float angulo in angulos)
        {
            Quaternion rotacion = bulletShot.rotation * Quaternion.Euler(0, angulo, 0);
            GameObject bala = Instantiate(shotgunBulletPrefab, bulletShot.position, rotacion);

            Rigidbody rbBala = bala.GetComponent<Rigidbody>();
            if (rbBala != null)
            {
                rbBala.linearVelocity = bala.transform.forward * bulletSpeed;
            }
        }
    }*/

    void ShootShotgun()
    {
        for (int i = 0; i < pellets; i++)
        {
            Quaternion spreadRot = bulletShot.rotation *
                Quaternion.Euler(Random.Range(-spreadAngle, spreadAngle),
                                 Random.Range(-spreadAngle, spreadAngle),
                                 0f);

            Ray ray = new Ray(bulletShot.position, spreadRot * Vector3.forward);
            RaycastHit hit;

            Vector3 endPoint = bulletShot.position + ray.direction * range;

            if (Physics.Raycast(ray, out hit, range))
            {
                endPoint = hit.point;

                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
                    if (enemy != null)
                    {
                        float dañoFinal = damage;
                        if (GameManager.Instance?.playerHealth != null)
                            dañoFinal *= GameManager.Instance.playerHealth.multiplicadorDaño;
                        enemy.RecibirDaño((int)dañoFinal);
                    }
                }
            }

            // Crea el tracer visual
            GameObject tracer = Instantiate(tracerPrefab);
            LineRenderer lr = tracer.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, bulletShot.position);
                lr.SetPosition(1, endPoint);
            }
            Destroy(tracer, 0.05f);
        }
    }

    // Llamar cada vez que cambies de arma
    void SetActiveWeapon(GameObject activeWeaponModel)
    {
        // Apagar todos
        pistolModel.SetActive(false);
        shotgunModel.SetActive(false);
        rifleModel.SetActive(false);
        sniperModel.SetActive(false);

        // Encender el que corresponde
        if(activeWeaponModel != null)
            activeWeaponModel.SetActive(true);
    }

    public void EquipPistol()
    {
        currentGunPrefab = pistolBulletPrefab;
        bulletSpeed = 25f;
        Debug.Log("Ahora tienes una pistola.");

        SetActiveWeapon(pistolModel);
        MostrarUIArmaActual(uiPistol);
    }

    public void EquipShotgun()
    {
        currentGunPrefab = shotgunBulletPrefab;
        bulletSpeed = 18f;
        Debug.Log("Ahora tienes una escopeta.");

        SetActiveWeapon(shotgunModel);
        MostrarUIArmaActual(uiShotgun);
        StopAllCoroutines();
        StartCoroutine(ShotgunTimer());
    }

    public void EquipRifle()
    {
        currentGunPrefab = rifleBulletPrefab;
        bulletSpeed = 40f;
        Debug.Log("Ahora tienes un fusil de asalto.");

        SetActiveWeapon(rifleModel);
        MostrarUIArmaActual(uiRifle);
        StopAllCoroutines();
        StartCoroutine(RifleTimer());
    }

    public void EquipSniper()
    {
        currentGunPrefab = sniperBulletPrefab;
        bulletSpeed = 70f;
        Debug.Log("Ahora tienes un rifle de francotirador.");

        SetActiveWeapon(sniperModel);
        MostrarUIArmaActual(uiSniper);
        StopAllCoroutines();
        StartCoroutine(SniperTimer());
    }

    public void AddDecoy()
    {
        Debug.Log("Has recogido un señuelo.");
        numDecoy++;
        ActualizarSeñueloUI();
    }

    public void ActualizarSeñueloUI()
    {
        if(decoyText != null)
        {
            decoyText.text = $"Señuelos: {numDecoy}";
        }
    }
    public void LanzarDecoy()
    {
        if (decoyPrefab == null)
        {
            Debug.LogWarning("Prefab del señuelo no asignado.");
            return;
        }

        // Crear el señuelo frente al jugador
        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        GameObject newDecoy = Instantiate(decoyPrefab, spawnPos, Quaternion.identity);

        // Aplicar fuerza para lanzarlo
        Rigidbody rb = newDecoy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        }

        numDecoy--;
        ActualizarSeñueloUI();
        Debug.Log($"Señuelo lanzado, ahora te quedan {numDecoy}");
    }

    void TryShove()
    {
        if (Time.time < lastShoveTime + shoveCooldown)
            return;

        lastShoveTime = Time.time;

        Vector3 boxCenter = transform.position + transform.forward * (shoveBoxSize.z * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            shoveBoxSize * 0.5f,
            transform.rotation
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(enemigoTag))
            {
                EmpujarEnemigo(hit);
            }
        }
    }

    void EmpujarEnemigo(Collider enemy)
    {
        Rigidbody rb = enemy.attachedRigidbody;
        if (rb == null)
            return;

        PlayerHealth stats = GetComponent<PlayerHealth>();

        float fuerzaEmpuje = shoveForce;
        float dañoExtra = 0f;

        if (stats != null)
        {
            fuerzaEmpuje *= stats.multiplicadorEmpuje;
            dañoExtra = stats.dañoEmpuje;
        }

        Vector3 direccion = (enemy.transform.position - transform.position).normalized;

        rb.AddForce(direccion * shoveForce, ForceMode.Impulse);

        if (dañoExtra > 0f)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                int dañoInt = Mathf.CeilToInt(dañoExtra);

                enemyHealth.RecibirDaño(dañoInt);
            }
        }

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.Stun(1f);   // Aturdir durante 1 segundo
        }
    }

    void TryDash()
    {
        if (isDashing) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        lastDashTime = Time.time;

        // Si el jugador no se está moviendo esquiva adelante
        Vector3 dashDir = moveInput.magnitude > 0.1f ? moveInput.normalized : transform.forward;

        StartCoroutine(DashRoutine(dashDir));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 boxCenter = transform.position + transform.forward * (shoveBoxSize.z * 0.5f);

        Gizmos.matrix = Matrix4x4.TRS(
            boxCenter,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, shoveBoxSize);
    }

    private IEnumerator ShotgunTimer()
    {
        float time = shotgunTimer;

        while (time > 0)
        {
            timerShotgunText.text = time.ToString("0.0");
            time -= Time.deltaTime;
            yield return null;
        }

        EquipPistol();
    }

    private IEnumerator RifleTimer()
    {
        float time = rifleTimer;

        while (time > 0)
        {
            timerRifleText.text = time.ToString("0.0");
            time -= Time.deltaTime;
            yield return null;
        }

        EquipPistol();
    }

    private IEnumerator SniperTimer()
    {
        float time = sniperTimer;

        while (time > 0)
        {
            timerSniperText.text = time.ToString("0.0");
            time -= Time.deltaTime;
            yield return null;
        }

        EquipPistol();
    }

    IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;
        isInvulnerable = true;

        float elapsed = 0f;
        Vector3 startPos = rb.position;
        Vector3 endPos = startPos + direction * dashDistance;

        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            rb.MovePosition(Vector3.Lerp(startPos, endPos, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.MovePosition(endPos);
        isDashing = false;
        isInvulnerable = false;
    }

    void PlayWeaponSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void MostrarUIArmaActual(GameObject objetoUI)
    {
        uiPistol.SetActive(false);
        uiShotgun.SetActive(false);
        uiRifle.SetActive(false);
        uiSniper.SetActive(false);

        objetoUI.SetActive(true);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
        {
            // Normal de la colisión (dirección hacia afuera del obstáculo)
            Vector3 normal = collision.contacts[0].normal;

            // Pequeño desplazamiento hacia afuera para evitar incrustarse
            rb.MovePosition(rb.position + normal * 0.15f);

            // Cancelar velocidad para evitar seguir presionando contra la pared
            rb.linearVelocity = Vector3.zero;
        }
    }

}
