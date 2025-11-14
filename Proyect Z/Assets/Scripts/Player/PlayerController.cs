using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 9f;

    [Header("Prefabs")]
    public GameObject pistolBulletPrefab;      // Bala normal (click izquierdo)
    public GameObject shotgunBulletPrefab;     // Balas de escopeta
    public GameObject rifleBulletPrefab;       // Balas de fusil de asalto
    public GameObject sniperBulletPrefab;      // Balas de francotirador
    public GameObject bulletPushPrefab;        // Bala que empuja (click derecho)
    public GameObject decoyPrefab;             // Señuelo
    
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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pistolSFX;
    public AudioClip shotgunSFX;
    public AudioClip rifleSFX;
    public AudioClip sniperSFX;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        currentGunPrefab = pistolBulletPrefab; // Empieza con pistola
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        // Disparo principal
        ComprobarArma();

        // Click derecho: bala que empuja (con retardo)
        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + pushFireDelay;
            Shoot(bulletPushPrefab);
        }

        // Lanzamiento de señuelo
        if(Input.GetKeyDown(KeyCode.G) && numDecoy > 0)
        {
            LanzarDecoy();
        }
    }

    void FixedUpdate()
    {
        if (moveInput.magnitude > 0f)
        {
            Vector3 movimiento = moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movimiento);
        }
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

    public void EquipPistol()
    {
        currentGunPrefab = pistolBulletPrefab;
        bulletSpeed = 25f;
        Debug.Log("Ahora tienes una pistola.");
    }

    public void EquipShotgun()
    {
        currentGunPrefab = shotgunBulletPrefab;
        bulletSpeed = 18f;
        Debug.Log("Ahora tienes una escopeta");
        StopAllCoroutines();
        StartCoroutine(ShotgunTimer());
    }

    public void EquipRifle()
    {
        currentGunPrefab = rifleBulletPrefab;
        bulletSpeed = 40f;
        Debug.Log("Ahora tienes un fusil de asalto");
        StopAllCoroutines();
        StartCoroutine(RifleTimer());
    }
    public void EquipSniper()
    {
        currentGunPrefab = sniperBulletPrefab;
        bulletSpeed = 70f;
        Debug.Log("Ahora tienes un rifle de francotirador");
        StopAllCoroutines();
        StartCoroutine(SniperTimer());
    }

    public void AddDecoy()
    {
        Debug.Log("Has recogido un señuelo.");
        numDecoy++;
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

        Debug.Log($"Señuelo lanzado, ahora te quedan {numDecoy}");
    }
    private IEnumerator ShotgunTimer()
    {
        yield return new WaitForSeconds(shotgunTimer);
        EquipPistol();
    }

    private IEnumerator RifleTimer()
    {
        yield return new WaitForSeconds(rifleTimer);
        EquipPistol();
    }

    private IEnumerator SniperTimer()
    {
        yield return new WaitForSeconds(sniperTimer);
        EquipPistol();
    }

    void PlayWeaponSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
