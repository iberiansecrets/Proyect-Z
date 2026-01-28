using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public float healAmount = 20f; // cantidad de vida que cura

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            ObjectSpawner ob = FindObjectOfType<ObjectSpawner>();
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
                Destroy(gameObject); // desaparece al ser recogido
                ob.vidaGenerada = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 90 * Time.deltaTime, 0);
    }
}
