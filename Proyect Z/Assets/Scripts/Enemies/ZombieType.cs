using UnityEngine;

public class ZombieType : MonoBehaviour
{
    public enum Tipo
    {
        Normal,
        Corredor,
        Colosal,
        Comandante
    }

    public Tipo tipo;
}
