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
    public GameObject bulletPushPrefab;        // Bala que empuja (click derecho)
    public Transform bulletShot;               // Punto desde donde se dispara

    public float bulletSpeed = 20f;

    public float shotgunAngle = 10f;           // Grados de dispersión para izquierda y derecha
    public int shotgunPellets = 3;             // Balas que lanza la escopeta

    [Header("Delays")]
    public float shotgunFireDelay = 0.6f;      // Tiempo entre disparos de escopeta
    public float pistolFireDelay = 0.3f;       // Tiempo entre disparos de pistola
    public float pushFireDelay = 0.8f;         // Tiempo entre empujes
    public float rifleFireDelay = 0.2f;        // Tiempo entre disparos de balas de fusil
    public float sniperFireDelay = 1.2f;        // Tiempo entre disparos de francotirador
        
    private float nextFireTime = 0f;           // Control de cadencia de disparo del fusil

    [Header("Temporizadores de armas")]
    private float shotgunTimer = 10f;
    private float rifleTimer = 8f;

    private GameObject currentGunPrefab;       // Prefab del arma actual
    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
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
                nextFireTime = Time.time + rifleFireTime;
                Shoot(currentGunPrefab);
            }
        }
        // Escopeta con retardo
        else if (currentGunPrefab == shotgunBulletPrefab)
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + shotgunFireDelay;
                ShootShotgun();
            }
        }
        // Pistola semiautomática con retardo
        else if (currentGunPrefab == pistolBulletPrefab)
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + pistolFireDelay;
                Shoot(currentGunPrefab);
            }
        }
    }

    void Shoot(GameObject prefab)
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
    }

    void ShootShotgun()
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
    }


    public void EquipPistol()
    {
        currentGunPrefab = pistolBulletPrefab;
        bulletSpeed = 20f;
        Debug.Log("Ahora tienes una pistola.");
    }

    public void EquipShotgun()
    {
        currentGunPrefab = shotgunBulletPrefab;
        bulletSpeed = 15f;
        Debug.Log("Ahora tienes una escopeta");
        StopAllCoroutines();
        StartCoroutine(ShotgunTimer());
    }

    public void EquipRifle()
    {
        currentGunPrefab = rifleBulletPrefab;
        bulletSpeed = 30f;
        Debug.Log("Ahora tienes un fusil de asalto");
        StopAllCoroutines();
        StartCoroutine(RifleTimer());
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
}
