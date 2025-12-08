using UnityEngine;

public class DecoyPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.AddDecoy();
            }

            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(0, 90 * Time.deltaTime, 0);
    }
}

