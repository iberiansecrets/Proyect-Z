using UnityEngine;

public class SniperPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.EquipSniper();
            }

            Destroy(gameObject);
        }
    }

    void Update()
    {
        transform.Rotate(0, 90 * Time.deltaTime, 0);
    }
}
