using BehaviourAPI.Core.Perceptions;
using UnityEngine;

public class AnglePerceptionCustom : Perception
{
    Transform _self;
    Transform _target;
    float _maxAngle;

    public AnglePerceptionCustom(Transform self, Transform target, float maxAngle)
    {
        _self = self;
        _target = target;
        _maxAngle = maxAngle;
    }

    public override bool Check()
    {
        Vector3 dir = (_target.position - _self.position).normalized;
        float angle = Vector3.Angle(_self.forward, dir);
        return angle <= _maxAngle * 0.5f;
    }
}
