using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 9f;
    public GameObject bulletPrefab;
    public Transform bulletShot;
    public float bulletSpeed = 20f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        if (Input.GetButtonDown("Fire1")) {
            GameObject bala = Instantiate(bulletPrefab, bulletShot.position, bulletShot.rotation);
            Rigidbody rb = bala.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = bulletShot.forward * bulletSpeed;
            }
        }
    }
}
