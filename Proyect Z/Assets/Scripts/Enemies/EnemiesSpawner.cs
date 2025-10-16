using UnityEngine;
using System.Collections.Generic;

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Configuraci�n general")]
    [SerializeField] private GameObject zombiePrefab; //Prefab del zombie
    [SerializeField] private int maxZombies = 10; //M�ximo de zombies creados (por defecto, es 10)
    [SerializeField] private float spawnInterval = 3f; //Tiempo entre apariciones (por defecto, 3 segundos)

    [Header("Puntos de aparici�n (SpawnPoints")]
    [SerializeField] private Transform[] spawnPoints; //Lista de los SpawnPoints

    //Variables
    private float timer = 0f;
    private List<GameObject> zombiesSpawned = new List<GameObject>();

    private void Update()
    {
        timer += Time.deltaTime;

        //Si ha pasado el tiempo necesario, spawn de un zombie. �OJO! No debe sobrepasar el m�ximo.
        if(timer > spawnInterval && zombiesSpawned.Count < maxZombies)
        {
            SpawnZombie();
            timer = 0f;
        }
    }

    private void SpawnZombie()
    {
        //Comprobar que hay puntos de respawn en la lista
        if(spawnPoints.Length == 0 || zombiePrefab == null)
        {
            Debug.Log("No hay puntos de respawn en el mapa. A�adelos a la lista.");
            return;
        }

        //Pilla cualquier punto de spawn
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        //Instancia al zombie desde ah�
        GameObject newZombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombiesSpawned.Add(newZombie);

        // Notificar al GameManager que hay un nuevo enemigo
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarEnemigo(newZombie);
    }

    //M�todo usado �nicamente como DEBUG. Elimina a todos los zombies de la escena.
    public void DestroyAllZombies()
    {
        foreach(var zombie in zombiesSpawned)
        {
            if(zombie != null) Destroy(zombie);
        }
        zombiesSpawned.Clear();
    }
}
