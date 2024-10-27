using ECM2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetCharacter : Character
{
    [Space(15f)]
    public Transform planetTransform;

    protected override void UpdateRotation(float deltaTime) {
        // Call base method (i.e: rotate towards movement direction)

        base.UpdateRotation(deltaTime);

        // Adjust gravity direction (ie: a vector pointing from character position to planet's center)

        Vector3 toPlanet = planetTransform.position - GetPosition();
        SetGravityVector(toPlanet.normalized * GetGravityMagnitude());

        // Adjust Character's rotation following the new world-up (defined by gravity direction)

        Vector3 worldUp = GetGravityDirection() * -1.0f;
        Quaternion newRotation = Quaternion.FromToRotation(GetUpVector(), worldUp) * GetRotation();

        SetRotation(newRotation);
    }
}
