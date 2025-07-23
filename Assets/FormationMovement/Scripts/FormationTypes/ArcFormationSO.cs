using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "ArcFormation", menuName = "Formation Movement/Arc")]
    public class ArcFormationSO : FormationTypeSO
    {
        [Space]
        [Header("Arc Setup")]
        [SerializeField] private float arcAngle;
        
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();

            // compute arc radius based on distance between followers
            var arcRadians = arcAngle * Mathf.Deg2Rad;
            var arcLength = distanceBetweenFollowers * (followersCount - 1);
            var radius = arcLength / arcRadians;

            var startAngle = -arcAngle / 2f;

            for (var i = 0; i < followersCount; i++)
            {
                var angleDeg = startAngle + (arcAngle / (followersCount - 1)) * i;
                var angleRad = angleDeg * Mathf.Deg2Rad;

                var x = Mathf.Sin(angleRad) * radius;
                var z = Mathf.Cos(angleRad) * radius - (distanceBetweenFollowers);

                // Depending on FollowersCount, increase LeaderZOffset in order to be sure
                // that the leader is placed ahead of the formation
                var offset = new Vector3(x, 0f, z - LeaderZOffset);
                offsets.Add(offset);
            }

            return offsets;
        }

        public override FormationLayout GetFormationLayout(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = GetSpawnOffsets(followersCount, distanceBetweenFollowers);
            var visualLeaderIndex = followersCount / 2;
            var layout = new FormationLayout(offsets, visualLeaderIndex);
            return layout;
        }
    }
}