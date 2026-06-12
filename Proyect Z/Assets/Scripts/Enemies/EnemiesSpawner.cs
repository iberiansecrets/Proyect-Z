using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private List<GameObject> zombiesPrefab = new List<GameObject>();
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxZombiesSimultaneos = 10;

    [Header("Puntos de aparición (SpawnPoints)")]
    [SerializeField] private Transform[] spawnPoints;

    // Variables internas
    public List<GameObject> zombiesSpawned = new List<GameObject>();
    private bool spawningActive = false;
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

        Debug.Log($"Cantidad de zombies generada en ronda {oleadaActual}: {cantidad}");
    }

    private IEnumerator SpawnRoutine(int cantidad)
    {
        spawningActive = true;
        zombiesSpawned.Clear();

        int zombiesPorSpawnear = cantidad;

        // Comandantes en rondas pares (2, 4, 6, 8 y 10)
        if (oleadaActual % 2 == 0)
        {
            SpawnZombieTipo(ZombieType.Tipo.Comandante, false);
            zombiesPorSpawnear--;
        }
        // Colosales en rondas pares altas (9, 10)
        else if (oleadaActual == 7|| oleadaActual == 9 || oleadaActual >= 10)
        {
            SpawnZombieTipo(ZombieType.Tipo.Colosal, false);
            zombiesPorSpawnear--;
        }

        // Spawn de la horda
        for (int i = 0; i < zombiesPorSpawnear; i++)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Evitar superar el límite simultáneo de zombies vivos
            if (zombiesSpawned.Count >= maxZombiesSimultaneos)
            {
                yield return new WaitUntil(() => zombiesSpawned.Count < maxZombiesSimultaneos);
            }

            SpawnZombieBasico();
        }

        spawningActive = false;
    }

    private void SpawnZombieBasico()
    {
        // 50% de probabilidad de que sea Normal, 50% Corredor
        ZombieType.Tipo tipoElegido = (Random.value > 0.5f) ? ZombieType.Tipo.Normal : ZombieType.Tipo.Corredor;
        SpawnZombieTipo(tipoElegido, false);
    }

    // Método maestro unificado para generar el zombie que necesitemos y aplicarle las subidas de stats
    public void SpawnZombieTipo(ZombieType.Tipo tipoBuscado, bool esInvocado = false)
    {
        if (spawnPoints.Length == 0 || zombiesPrefab.Count == 0) return;

        // Buscamos en la lista el prefab que tenga el ZombieType que estamos pidiendo
        GameObject prefab = zombiesPrefab.Find(z =>
        {
            var t = z.GetComponent<ZombieType>();
            return t != null && t.tipo == tipoBuscado;
        });

        if (prefab == null)
        {
            Debug.LogWarning($"[Spawner] No se encontró prefab del tipo {tipoBuscado}. Revisa tu lista en el Inspector.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject newZombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawned.Add(newZombie);

        // Registro en GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie, esInvocado);

        // Escalado de vida: los zombies obtienen un 15% de vida máxima
        EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float multiplicadorVida = 1f + (oleadaActual - 1) * 0.15f;
            health.vidaMaxima *= multiplicadorVida;
            health.vidaActual = health.vidaMaxima; // Recargamos la vida actual a su nuevo máximo

            health.onDeath += () => OnZombieDeath(newZombie);
        }
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

        if (GameManager.Instance != null)
            GameManager.Instance.EnemigoDerrotado();
    }

    // Usado por el Comandante o penalizaciones. Solo invoca Normales o Corredores para protegerle.
    public void SpawnBalanceado(int cantidad)
    {
        int normales = GetNumNormales();
        int corredores = GetNumCorredores();
        int total = Mathf.Max(1, normales + corredores);

        int spawnNormales = Mathf.RoundToInt(cantidad * (normales / (float)total));
        int spawnCorredores = cantidad - spawnNormales; // Rellena para asegurar la cantidad exacta pedida

        for (int i = 0; i < spawnNormales; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Normal, true); // esInvocado = true
        }
        for (int i = 0; i < spawnCorredores; i++)
        {
            SpawnZombieTipo(ZombieType.Tipo.Corredor, true); // esInvocado = true
        }
    }

    public void DestroyAllZombies()
    {
        foreach (var zombie in zombiesSpawned)
        {
            if (zombie != null) Destroy(zombie);
        }
        zombiesSpawned.Clear();
    }

    public int GetNumNormales() => zombiesSpawned.FindAll(z => z?.GetComponent<ZombieType>()?.tipo == ZombieType.Tipo.Normal).Count;
    public int GetNumColosales() => zombiesSpawned.FindAll(z => z?.GetComponent<ZombieType>()?.tipo == ZombieType.Tipo.Colosal).Count;
    public int GetNumCorredores() => zombiesSpawned.FindAll(z => z?.GetComponent<ZombieType>()?.tipo == ZombieType.Tipo.Corredor).Count;
    public int GetNumComandantes() => zombiesSpawned.FindAll(z => z?.GetComponent<ZombieType>()?.tipo == ZombieType.Tipo.Comandante).Count;

    public int GetNumZombies()
    {
        return GetNumColosales() + GetNumCorredores() + GetNumNormales() + GetNumComandantes();
    }
}