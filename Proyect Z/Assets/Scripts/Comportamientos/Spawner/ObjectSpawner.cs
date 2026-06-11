using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

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
    public GameObject decoyPrefab;

    [Header("Área de aparición (Circular e Inteligente)")]
    public Vector3 areaCenter = Vector3.zero;
    [Tooltip("El componente X se usará como el RADIO del círculo de spawn (ej: 30)")]
    public Vector3 areaSize = new Vector3(30f, 0f, 30f);

    [Header("Parámetros")]
    public int umbralZombies = 5;       // Límite de zombies para "presión alta"   

    public int minEscopeta = 0;            // Minimo para normalizar
    public int numEscopeta = 0;            // Numero de armas spawneadas
    public int maxEscopeta = 10;            // Limite de spawns

    public int minRifle = 0;            // Minimo para normalizar
    public int numRifle = 0;            // Numero de armas spawneadas
    public int maxRifle = 10;            // Limite de spawns

    public int minFranco = 0;            // Minimo para normalizar
    public int numFranco = 0;            // Numero de armas spawneadas
    public int maxFranco = 10;            // Limite de spawns

    [HideInInspector] public bool vidaGenerada = false;
    [HideInInspector] public bool armaGenerada = false;
    [HideInInspector] public bool penalizando = false;
    [HideInInspector] public bool senueloGenerado = false;

    private readonly List<GameObject> objetosActivos = new();

    private void Update()
    {
        EnemiesSpawner es = FindAnyObjectByType<EnemiesSpawner>();
        Debug.Log($"Hay {es.GetNumZombies()} zombies");
    }

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
        Debug.Log($"Muchos zombies: {es.GetNumZombies() >= umbralZombies}");
        return es.GetNumZombies() >= umbralZombies;
    }

    public bool PocosZombies()
    {
        if (gameManager == null) return false;
        EnemiesSpawner es = FindFirstObjectByType<EnemiesSpawner>();
        Debug.Log($"Pocos zombies: {es.GetNumZombies() <= umbralZombies}");
        return es.GetNumZombies() <= umbralZombies;
    }

    // Asegura que saca las armas dentro del NavMesh 
    private Vector3 ObtenerPuntoCircularNavMesh()
    {
        float radioMaximo = areaSize.x; // Usamos el componente X como radio del coliseo (ej: 30f para un círculo de 60x60)

        // Generamos un punto aleatorio bidimensional dentro de un círculo perfecto
        Vector2 puntoCirculo = Random.insideUnitCircle * radioMaximo;
        Vector3 posicionTentativa = areaCenter + new Vector3(puntoCirculo.x, 0f, puntoCirculo.y);

        // Muestreamos el NavMesh en esa zona para encontrar el suelo legal y transitable más cercano
        NavMeshHit hit;
        // Buscamos en un rango generoso para asegurar que si cae en una pared, lo devuelva a la arena de juego
        if (NavMesh.SamplePosition(posicionTentativa, out hit, radioMaximo, NavMesh.AllAreas))
        {
            return hit.position; // Retorna la posición exacta sobre la malla azul del NavMesh
        }

        return posicionTentativa; // Si algo falla, devuelve el auxiliar
    }

    public void SpawnBotiquin()
    {
        if (medkitPrefab == null) return;

        // Cambiado el cálculo cuadrado por nuestra función circular en NavMesh
        Vector3 randomPos = ObtenerPuntoCircularNavMesh();

        GameObject nuevo = Instantiate(medkitPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        vidaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {medkitPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnDecoy()
    {
        if (decoyPrefab == null) return;

        // Cambiado el cálculo cuadrado por nuestra función circular en NavMesh
        Vector3 randomPos = ObtenerPuntoCircularNavMesh();

        GameObject nuevo = Instantiate(decoyPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        senueloGenerado = true;
        Debug.Log($"[ObjectSpawner] ha generado: {decoyPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }
    public void SpawnEscopeta()
    {
        if (shotgunPrefab == null || numEscopeta >= 10) return;

        // Cambiado el cálculo cuadrado por nuestra función circular en NavMesh
        Vector3 randomPos = ObtenerPuntoCircularNavMesh();

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

        // Cambiado el cálculo cuadrado por nuestra función circular en NavMesh
        Vector3 randomPos = ObtenerPuntoCircularNavMesh();

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

        // Cambiado el cálculo cuadrado por nuestra función circular en NavMesh
        Vector3 randomPos = ObtenerPuntoCircularNavMesh();

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

    // Modificado para mostrar el nuevo área circular real en el editor de Unity
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.35f);
        // Dibujamos un disco plano para visualizar perfectamente el área circular dentro del coliseo
        Gizmos.DrawWireSphere(areaCenter, areaSize.x);
    }

    public bool MuchoTiempoSinMatar()
    {
        if (gameManager == null) return false;
        Debug.Log($"Mucho sin matar: {gameManager.MuchoSinMatar()}");
        return gameManager.MuchoSinMatar();
    }

    public void PenalizacionTiempo()
    {
        if (penalizando) return;
        penalizando = true;
        EnemiesSpawner es = FindAnyObjectByType<EnemiesSpawner>();
        es.SpawnBalanceado(6);

        StartCoroutine(ResetPenalizacion());
    }

    private IEnumerator ResetPenalizacion()
    {
        yield return new WaitForSeconds(5f);
        penalizando = false;
    }

    public bool GetVidaGenerada()
    {
        return !vidaGenerada;
    }

    public bool GetArmaGenerada()
    {
        return !armaGenerada;
    }

    public bool GetDecoyGenerado()
    {
        return !senueloGenerado;
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