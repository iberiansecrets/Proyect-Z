using UnityEngine;

public class RiflePickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            ObjectSpawner ob = FindObjectOfType<ObjectSpawner>();
            if (pc != null)
            {
                pc.EquipRifle();
            }
            ob.armaGenerada = false;
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(0, 90 * Time.deltaTime, 0);
    }
}
