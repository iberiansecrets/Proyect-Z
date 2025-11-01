using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 9f;

    [Header("Disparo")]
    public GameObject pistolBulletPrefab;          // Bala normal (click izquierdo)

    public GameObject shotgunBulletPrefab;      // Balas de escopeta
    public float shotgunAngle = 10f; // Grados de dispersión para izquierda y derecha
    public int shotgunPellets = 3; // Balas que lanza la escopeta

    private GameObject currentGunPrefab; // Prefab del arma actual
    public GameObject bulletPushPrefab;      // Bala que empuja (click derecho)
    public Transform bulletShot;             // Punto desde donde se dispara
    public float bulletSpeed = 20f;

    [Header("Temporizadores")]
    private float shotgunTimer = 10f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        //Al iniciar el juego, el personaje siempre empieza con pistola
        currentGunPrefab = pistolBulletPrefab;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        // Click izquierdo: bala actual
        if (Input.GetButtonDown("Fire1"))
        {
            if(currentGunPrefab == shotgunBulletPrefab)
            {
                ShootShotgun();
            }
            else
            {
                Shoot(currentGunPrefab);
            }
        }

        // Click derecho: bala que empuja
        if (Input.GetButtonDown("Fire2"))
        {
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

    void Shoot(GameObject prefab)
    {
        // Evita errores si no hay prefab asignado
        if (prefab == null || bulletShot == null)
        {
            Debug.LogWarning(" Prefab o bulletShot no asignado en PlayerController.");
            return;
        }

        // Instanciar la bala
        GameObject bala = Instantiate(prefab, bulletShot.position, bulletShot.rotation);

        // Asignar velocidad inicial
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
        //Ponemos las el "ángulo" de las balas
        float[] angulos = { -shotgunAngle, 0, shotgunAngle };

        //Disparamos las balas de la escopeta a la vez 
        foreach (float angulo in angulos)
        {
            Quaternion rotacion = bulletShot.rotation * Quaternion.Euler(0, angulo, 0);

            GameObject bala = Instantiate(shotgunBulletPrefab, bulletShot.position, rotacion);
            Rigidbody rbBala = bala.GetComponent<Rigidbody>();

            if(rbBala != null)
            {
                rbBala.linearVelocity = bala.transform.forward * bulletSpeed;
            }
        }
    }

    public void EquipPistol()
    {
        currentGunPrefab = pistolBulletPrefab;
        Debug.Log("Ahora tienes una pistola.");
    }

    public void EquipShotgun()
    {
        currentGunPrefab = shotgunBulletPrefab;
        Debug.Log("Ahora tienes una escopeta");
        StopAllCoroutines();
        StartCoroutine(ShotgunTimer());
    }
    private IEnumerator ShotgunTimer()
    {
        yield return new WaitForSeconds(shotgunTimer);
        EquipPistol();
    }
}
