using UnityEngine;
using System.Collections;

public class ShotgunController : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public int pellets = 12; // cuántos rayos disparamos
    public float spreadAngle = 15f; // ángulo total del cono de disparo
    public float range = 15f; // distancia máxima del disparo
    public float damage = 15f;
    public float fireDelay = 1f;

    [Header("Efectos Visuales")]
    public GameObject tracerPrefab; // prefab del tracer
    public Transform firePoint; // punto desde el que salen los disparos
    public LayerMask hitMask; // capa de enemigos

    private bool canFire = true;

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && canFire)
        {
            StartCoroutine(FireShotgun());
        }
    }

    IEnumerator FireShotgun()
    {
        canFire = false;

        for (int i = 0; i < pellets; i++)
        {
            // Generar dirección con dispersión
            Vector3 direction = GetSpreadDirection();

            if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range, hitMask))
            {
                // Daño al enemigo
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
                    if (enemy != null)
                    {
                        float dañoFinal = damage;

                        if (GameManager.Instance != null && GameManager.Instance.playerHealth != null)
                            dañoFinal *= GameManager.Instance.playerHealth.multiplicadorDaño;

                        enemy.RecibirDaño((int)dañoFinal);
                    }
                }

                // Efecto visual del tracer
                if (tracerPrefab != null)
                {
                    GameObject tracer = Instantiate(tracerPrefab, firePoint.position, Quaternion.identity);
                    LineRenderer lr = tracer.GetComponent<LineRenderer>();

                    if (lr != null)
                    {
                        lr.SetPosition(0, firePoint.position);
                        lr.SetPosition(1, hit.point);
                    }

                    Destroy(tracer, 0.1f); // eliminar tras un breve tiempo
                }
            }
            else
            {
                if (tracerPrefab != null)
                {
                    GameObject tracer = Instantiate(tracerPrefab, firePoint.position, Quaternion.identity);
                    LineRenderer lr = tracer.GetComponent<LineRenderer>();

                    if (lr != null)
                    {
                        lr.SetPosition(0, firePoint.position);
                        lr.SetPosition(1, firePoint.position + direction * range);
                    }

                    Destroy(tracer, 0.1f);
                }
            }
        }

        yield return new WaitForSeconds(fireDelay);
        canFire = true;
    }

    private Vector3 GetSpreadDirection()
    {
        float angleX = Random.Range(-spreadAngle, spreadAngle);
        float angleY = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRotation = Quaternion.Euler(angleX, angleY, 0);
        return spreadRotation * firePoint.forward;
    }
}
