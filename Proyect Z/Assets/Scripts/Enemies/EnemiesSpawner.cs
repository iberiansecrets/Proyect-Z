using UnityEngine;
using System.Collections.Generic;

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private List<GameObject> zombiesPrefab = new List<GameObject>();
    [SerializeField] private int maxZombies = 10; //Máximo de zombies creados (por defecto, es 10)
    [SerializeField] private float spawnInterval = 3f; //Tiempo entre apariciones (por defecto, 3 segundos)

    [Header("Puntos de aparición (SpawnPoints")]
    [SerializeField] private Transform[] spawnPoints; //Lista de los SpawnPoints

    //Variables
    private float timer = 0f;
    private List<GameObject> zombiesSpawned = new List<GameObject>();

    private void Update()
    {
        timer += Time.deltaTime;

        //Si ha pasado el tiempo necesario, spawn de un zombie. ¡OJO! No debe sobrepasar el máximo.
        if(timer > spawnInterval && zombiesSpawned.Count < maxZombies)
        {
            SpawnZombie();
            timer = 0f;
        }
    }

    private void SpawnZombie()
    {
        //Comprobar que hay puntos de respawn en la lista
        if(spawnPoints.Length == 0 || zombiesPrefab.Count == 0)
        {
            Debug.Log("No hay puntos de respawn en el mapa. Añadelos a la lista.");
            return;
        }

        //Pilla cualquier punto de spawn
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        //Instancia al zombie desde ahí
        GameObject newZombie = Instantiate(zombiesPrefab[Random.Range(0,zombiesPrefab.Count)], spawnPoint.position, spawnPoint.rotation);
        zombiesSpawned.Add(newZombie);

        // Notificar al GameManager que hay un nuevo enemigo
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie);
    }

    //Método usado únicamente como DEBUG. Elimina a todos los zombies de la escena.
    public void DestroyAllZombies()
    {
        foreach(var zombie in zombiesSpawned)
        {
            if(zombie != null) Destroy(zombie);
        }
        zombiesSpawned.Clear();
    }
}
