using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private List<GameObject> zombiesPrefab = new List<GameObject>(); // Lista de prefabs de zombies
    [SerializeField] private float spawnInterval = 2f; // Tiempo entre apariciones (por defecto, 2 segundos)
    [SerializeField] private int maxZombiesSimultaneos = 10; // Máximo de zombies vivos al mismo tiempo

    [Header("Puntos de aparición (SpawnPoints)")]
    [SerializeField] private Transform[] spawnPoints; // Lista de puntos donde pueden aparecer zombies

    // Variables internas
    public List<GameObject> zombiesSpawned = new List<GameObject>();
    private bool spawningActive = false; // Controla si la oleada está activa

    public void GenerarOleada(int cantidad)
    {
        if (spawnPoints.Length == 0 || zombiesPrefab.Count == 0)
        {
            Debug.LogWarning("No hay puntos de spawn o prefabs de zombies asignados.");
            return;
        }

        // Si ya hay una oleada activa, detenerla antes de iniciar una nueva
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine(cantidad));

        Debug.Log($"Cantidad de zombies generada: {cantidad}");
    }

    private IEnumerator SpawnRoutine(int cantidad)
    {
        spawningActive = true;
        zombiesSpawned.Clear();

        for (int i = 0; i < cantidad; i++)
        {
            // Esperar un momento antes de cada spawn
            yield return new WaitForSeconds(spawnInterval);

            // Evitar superar el límite simultáneo de zombies vivos
            if (zombiesSpawned.Count < maxZombiesSimultaneos)
            {
                SpawnZombie();
            }
            else
            {
                // Esperar hasta que haya espacio disponible antes de seguir
                yield return new WaitUntil(() => zombiesSpawned.Count < maxZombiesSimultaneos);
                SpawnZombie();
            }
        }

        spawningActive = false;
    }

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombiesPrefab.Count == 0)
        {
            Debug.Log("No hay puntos de respawn en el mapa. Añádelos a la lista.");
            return;
        }

        // Escoge un punto de spawn aleatorio
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Instancia un tipo aleatorio de zombie
        GameObject zombiePrefab = zombiesPrefab[Random.Range(0, zombiesPrefab.Count)];
        GameObject newZombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawned.Add(newZombie);
        Debug.Log($"{zombiesSpawned.Count}");

        // Notificar al GameManager que hay un nuevo enemigo
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie);

        // Suscribirse al evento de muerte del zombie (si existe el componente EnemyHealth)
        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
            health.onDeath += () => OnZombieDeath(newZombie);
    }

    void SpawnZombieTipo(ZombieType.Tipo tipoBuscado)
    {
        GameObject prefab = zombiesPrefab.Find(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == tipoBuscado;
        });

        if (prefab == null)
        {
            Debug.LogWarning($"No se encontró prefab del tipo {tipoBuscado}");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject newZombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawned.Add(newZombie);

        // Notificar al GameManager que hay un nuevo enemigo
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie);

        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
            health.onDeath += () => OnZombieDeath(newZombie);
    }

    public void SpawnBalanceado(int cantidad)
    {
        Debug.Log("SPAWN BALANCEADO DETECTADO");
        int normales = GetNumNormales();
        Debug.Log(normales);
        int corredores = GetNumCorredores();
        Debug.Log(corredores);
        int colosales = GetNumColosales();
        Debug.Log(colosales);
        int total = Mathf.Max(1, normales + corredores + colosales);
        Debug.Log(total);

        int spawnNormales = Mathf.RoundToInt(cantidad * (normales / (float)total));
        int spawnCorredores = Mathf.RoundToInt(cantidad * (corredores / (float)total));
        int spawnColosales = Mathf.RoundToInt(cantidad * (colosales / (float)total));

        for (int i = 0; i < spawnNormales; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Normal);
            GameManager.Instance.RegistrarEnemigo(null);
        }
        for (int i = 0; i < spawnCorredores; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Corredor);
            GameManager.Instance.RegistrarEnemigo(null);
        }
        for (int i = 0; i < spawnColosales; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Colosal);
            GameManager.Instance.RegistrarEnemigo(null);
        }

        Debug.Log($"Se han generado: {spawnNormales} normales, {spawnCorredores} corredores y {spawnColosales} colosales");        
    }

    private void OnZombieDeath(GameObject zombie)
    {
        // Eliminarlo de la lista local
        if (zombiesSpawned.Contains(zombie))
            Debug.Log($"{zombiesSpawned.Count}");
            zombiesSpawned.Remove(zombie);

        // Notificar al GameManager que un enemigo ha muerto
        if (GameManager.Instance != null)
            GameManager.Instance.EnemigoDerrotado();
    }
    

    public void DestroyAllZombies()
    {
        foreach (var zombie in zombiesSpawned)
        {
            if (zombie != null)
                Destroy(zombie);
        }
        zombiesSpawned.Clear();
    }

    public int GetNumNormales()
    {
        return zombiesSpawned.FindAll(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Normal;
        }).Count;
    }

    public int GetNumColosales()
    {
        return zombiesSpawned.FindAll(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Colosal;
        }).Count;
    }

    public int GetNumCorredores()
    {
        return zombiesSpawned.FindAll(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Corredor;
        }).Count;
    }
}
