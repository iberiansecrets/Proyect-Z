using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 9f;

    [Header("Disparo")]
    public GameObject bulletPrefab;          // Bala normal (click izquierdo)
    public GameObject bulletPushPrefab;      // Bala que empuja (click derecho)
    public Transform bulletShot;             // Punto desde donde se dispara
    public float bulletSpeed = 20f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        // 🔹 Click izquierdo: bala normal
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot(bulletPrefab);
        }

        // 🔹 Click derecho: bala que empuja
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
}
