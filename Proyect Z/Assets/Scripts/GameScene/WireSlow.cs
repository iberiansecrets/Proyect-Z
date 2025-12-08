using UnityEngine;
using System.Collections.Generic;

public class WireSlow : MonoBehaviour
{
    [Header("Efectos del alambre")]
    public float slowMultiplier = 0.45f;
    public float damagePerSecond = 5f;

    private Dictionary<PlayerController, float> originalPlayerSpeed = new Dictionary<PlayerController, float>();
    private Dictionary<EnemyController, float> originalEnemySpeed = new Dictionary<EnemyController, float>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Si está en dash ignorar y no ralentizar
            if (player.isInvulnerable)
                return;

            if (!originalPlayerSpeed.ContainsKey(player))
                originalPlayerSpeed[player] = player.moveSpeed;

            player.moveSpeed *= slowMultiplier;
            return;
        }

        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            if (!originalEnemySpeed.ContainsKey(enemy))
                originalEnemySpeed[enemy] = enemy.speed;

            enemy.speed *= slowMultiplier;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.RecibirDaño(damagePerSecond * Time.deltaTime);
            return;
        }

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.RecibirDaño(Mathf.CeilToInt(damagePerSecond * Time.deltaTime));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (originalPlayerSpeed.ContainsKey(player))
            {
                player.moveSpeed = originalPlayerSpeed[player];
                originalPlayerSpeed.Remove(player);
            }
            return;
        }

        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            if (originalEnemySpeed.ContainsKey(enemy))
            {
                enemy.speed = originalEnemySpeed[enemy];
                originalEnemySpeed.Remove(enemy);
            }
        }
    }
}
