using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    private int oleadaActual = 1;

    public void GenerarOleada(int cantidad, int numeroOleada)
    {
        oleadaActual = numeroOleada;

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

        int zombiesPorSpawnear = cantidad;

        // CONTROL DE FLUJO: Si no hay Comandante, lo creamos y resta de la cantidad de la horda total
        if (!ExisteComandanteVivo())
        {
            //SpawnComander();
            zombiesPorSpawnear--; // El comandante cuenta como uno de los zombies de la ronda
        }

        for (int i = 0; i < zombiesPorSpawnear; i++)
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

    private void SpawnComander()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject newZombie = Instantiate(zombiesPrefab[3], spawnPoint.position, spawnPoint.rotation);
        zombiesSpawned.Add(newZombie);

        // El comandante es parte de la horda base, no se marca como esInvocado
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie, false);

        // Suscribirse al evento de muerte del zombie (si existe el componente EnemyHealth)
        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float multiplicadorVida = 1f + (oleadaActual - 1) * 0.2f; // Aumenta 20% por oleada
            health.vidaMaxima *= multiplicadorVida;
            health.onDeath += () => OnZombieDeath(newZombie);
        }

        EnemyController controller = newZombie.GetComponent<EnemyController>();
        if (controller != null)
        {
            float multiplicadorDaño = 1f + (oleadaActual - 1) * 0.15f; // Aumenta 15% por oleada
            controller.damage *= multiplicadorDaño;

            float multiplicadorVelocidad = 1f + (oleadaActual - 1) * 0.12f; // Aumenta 12% por oleada
            controller.speed *= multiplicadorVelocidad;
        }
    }

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombiesPrefab.Count == 0)
        {
            Debug.Log("No hay puntos de respawn en el mapa. Añádelos a la lista.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // FILTRO DE SEGURIDAD MÁXIMA: Excluimos la posición [3] (Comandante)
        GameObject zombiePrefab = zombiesPrefab[Random.Range(0, 3)];
        GameObject newZombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawned.Add(newZombie);

        // Los zombies comunes de la oleada base tampoco se marcan como esInvocado
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie, false);

        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
            health.onDeath += () => OnZombieDeath(newZombie);
    }

    public bool ExisteComandanteVivo()
    {
        return zombiesSpawned.Exists(z =>
        {
            if (z == null) return false;
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Comandante;
        });
    }

    private void OnZombieDeath(GameObject zombie)
    {
        if (zombiesSpawned.Contains(zombie))
        {
            zombiesSpawned.Remove(zombie);
        }

        // Notificar al GameManager que un enemigo ha muerto para que reste 1
        if (GameManager.Instance != null)
            GameManager.Instance.EnemigoDerrotado();
    }

    // El método que utiliza la habilidad del Comandante para invocar esbirros reales y registrados
    public void SpawnZombieTipo(ZombieType.Tipo tipoBuscado)
    {
        GameObject prefab = zombiesPrefab.Find(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == tipoBuscado;
        });

        if (prefab == null)
        {
            Debug.Log($"No se encontró prefab del tipo {tipoBuscado}");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject newZombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawned.Add(newZombie);

        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie, true);

        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
            health.onDeath += () => OnZombieDeath(newZombie);
    }

    public void SpawnBalanceado(int cantidad)
    {
        Debug.Log("SPAWN BALANCEADO DETECTADO");
        int normales = GetNumNormales();
        int corredores = GetNumCorredores();
        int colosales = GetNumColosales();
        int total = Mathf.Max(1, normales + corredores + colosales);

        int spawnNormales = Mathf.RoundToInt(cantidad * (normales / (float)total));
        int spawnCorredores = Mathf.RoundToInt(cantidad * (corredores / (float)total));
        int spawnColosales = Mathf.RoundToInt(cantidad * (colosales / (float)total));

        for (int i = 0; i < spawnNormales; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Normal);
        }
        for (int i = 0; i < spawnCorredores; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Corredor);
        }
        for (int i = 0; i < spawnColosales; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Colosal);
        }
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
        return zombiesSpawned.FindAll(z => {
            var t = z?.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Normal;
        }).Count;
    }

    public int GetNumColosales()
    {
        return zombiesSpawned.FindAll(z => {
            var t = z?.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Colosal;
        }).Count;
    }

    public int GetNumCorredores()
    {
        return zombiesSpawned.FindAll(z => {
            var t = z?.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Corredor;
        }).Count;
    }

    public int GetNumComandantes()
    {
        return zombiesSpawned.FindAll(z => {
            var t = z?.GetComponent<ZombieType>();
            return t != null && t.tipo == ZombieType.Tipo.Comandante;
        }).Count;
    }

    public int GetNumZombies()
    {        
        return GetNumColosales() + GetNumCorredores() + GetNumNormales() + GetNumComandantes();
    }
}