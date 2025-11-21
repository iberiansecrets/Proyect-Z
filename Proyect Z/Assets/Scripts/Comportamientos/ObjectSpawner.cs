using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth playerHealth;
    public GameManager gameManager;

    [Header("Prefabs de objetos")]
    public GameObject medkitPrefab;
    public GameObject shotgunPrefab;
    public GameObject riflePrefab;
    public GameObject sniperPrefab;

    [Header("Área de aparición")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(30f, 0f, 30f);

    [Header("Parámetros")]
    public int umbralZombies = 5;       // Límite de zombies para "presión alta"
    public int maxObjetosActivos = 3;    // Límite total de objetos en el mapa

    public int minEscopeta = 0;
    public int numEscopeta = 0;
    public int maxEscopeta = 10;

    public int minRifle = 0;
    public int numRifle = 0;
    public int maxRifle = 10;

    public int minFranco = 0;
    public int numFranco = 0;
    public int maxFranco = 10;


    [HideInInspector] public bool vidaGenerada = false;
    [HideInInspector] public bool armaGenerada = false;

    private readonly List<GameObject> objetosActivos = new();

    public bool VidaJugadorBaja()
    {
        if (playerHealth == null) return false;
        float ratio = playerHealth.GetVidaActual() / playerHealth.GetVidaMaxima();
        return ratio < 0.35f;
    }

    public bool MuchosZombies()
    {
        if (gameManager == null) return false;
        EnemiesSpawner es = FindFirstObjectByType<EnemiesSpawner>();
        Debug.Log($"{es.zombiesSpawned.Count >= umbralZombies}");
        return es.zombiesSpawned.Count >= umbralZombies;
    }    

    public void SpawnBotiquin()
    {
        if (medkitPrefab == null) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(medkitPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        vidaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {medkitPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnEscopeta()
    {
        if (shotgunPrefab == null || numEscopeta >= 10) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(shotgunPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        numEscopeta++;
        Debug.Log($"Se han generado {numEscopeta} escopetas");
        Debug.Log($"[ObjectSpawner] ha generado: {shotgunPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnFusil()
    {
        if (riflePrefab == null || numRifle >= 10) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(riflePrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        numRifle++;
        Debug.Log($"Se han generado {numRifle} rifles");
        Debug.Log($"[ObjectSpawner] ha generado: {riflePrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnFrancotirador()
    {
        if (sniperPrefab == null || numFranco >= 10) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(sniperPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        numFranco++;
        Debug.Log($"Se han generado {numFranco} francotiradores");
        Debug.Log($"[ObjectSpawner] ha generado: {sniperPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    private IEnumerator RemoveWhenDestroyed(GameObject obj)
    {
        yield return new WaitUntil(() => obj == null);
        objetosActivos.Remove(obj);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }

    public int numeroRandom()
    {
        int numero = Random.Range(0, 1);
        return numero;
    }

    public bool GetVidaGenerada()
    {
        return !vidaGenerada;
    }

    public bool GetArmaGenerada()
    {
        return !armaGenerada;
    }

    public float GetNumEscopeta()
    {
        float escopetaNormalizada = Normalizar(numEscopeta, minEscopeta, maxEscopeta);
        return escopetaNormalizada;
    }

    public float GetNumRifle()
    {
        float rifleNormalizado = Normalizar(numRifle, minRifle, maxRifle);
        return rifleNormalizado;
    }

    public float GetNumFranco()
    {
        float francoNormalizado = Normalizar(numFranco, minFranco, maxFranco);
        return francoNormalizado;
    }

    public float Normalizar(int valor, int min, int max)
    {
        return (float)(valor - min) / (max - min);
    }
}
