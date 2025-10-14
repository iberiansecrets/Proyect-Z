using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 9f;
    public GameObject bulletPrefab;
    public Transform bulletShot;
    public float bulletSpeed = 20f;

    private Rigidbody rb;
    private Vector3 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        if (Input.GetButtonDown("Fire1")) {
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        if (moveInput.magnitude > 0f)
        {
            Vector3 movimiento = moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movimiento);
        }
        else if (moveInput.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 0.1f);
        }
    }

    void Shoot()
    {
        GameObject bala = Instantiate(bulletPrefab, bulletShot.position, bulletShot.rotation);
        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = bulletShot.forward * bulletSpeed;
        }
    }
}
