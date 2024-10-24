using System.Collections.Generic;
using UnityEngine;

public static class ConeCastExtension
{
    public static RaycastHit[] ConeCastAll(this Physics physics, Vector3 origin, float maxRadius, Vector3 direction, float maxDistance, float coneAngle) {
        List<RaycastHit> coneCastHitList = new List<RaycastHit>();

        if (Physics.SphereCast(origin - new Vector3(0, 0, maxRadius), maxRadius, direction, out RaycastHit sphereCastHit, maxDistance)) {
            Vector3 hitPoint = sphereCastHit.point;
            Vector3 directionToHit = hitPoint - origin;
            float angleToHit = Vector3.Angle(direction, directionToHit);

            if (angleToHit < coneAngle) {
                coneCastHitList.Add(sphereCastHit);
            }
        }

        RaycastHit[] coneCastHits = new RaycastHit[coneCastHitList.Count];
        coneCastHits = coneCastHitList.ToArray();

        return coneCastHits;
    }
}