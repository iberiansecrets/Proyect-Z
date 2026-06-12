using BehaviourAPI.Core;
using BehaviourAPI.Core.Perceptions;
using UnityEngine;

public class OdorPerception : Perception
{
    private Transform colosalTransform;
    private float radioOlor;

    public OdorPerception(Transform colosal, float radio)
    {
        this.colosalTransform = colosal;
        this.radioOlor = radio;
    }

    public override bool Check()
    {
        if (PlayerOdorTrail.Instance == null || PlayerOdorTrail.Instance.rastroPosiciones.Count == 0)
            return false;

        // Comprobamos si el Colosal está cerca de alguna parte del rastro
        foreach (Vector3 puntoRastro in PlayerOdorTrail.Instance.rastroPosiciones)
        {
            if (Vector3.Distance(colosalTransform.position, puntoRastro) <= radioOlor)
            {
                return true; // ¡Ha olido el rastro!
            }
        }
        return false;
    }
}