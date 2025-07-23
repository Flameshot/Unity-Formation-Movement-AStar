using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "CircleFormation", menuName = "Formation Movement/Circle")]
    public class CircleFormationSO : FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var radius = followersCount * distanceBetweenFollowers / Mathf.PI;

            var offsets = new List<Vector3>();
            for (var i = 0; i < followersCount; i++)
            {
                var angle = i * 2f * Mathf.PI / followersCount;
                
                var offset = new Vector3(
                    radius * Mathf.Sin(angle),
                    0f,
                    -radius + radius * Mathf.Cos(angle)
                );
                
                offset += new Vector3(0f, 0f, -LeaderZOffset);
                offsets.Add(offset);
            }

            return offsets;        
        }

        public override FormationLayout GetFormationLayout(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = GetSpawnOffsets(followersCount, distanceBetweenFollowers);
            var visualLeaderIndex = 0;
            var layout = new FormationLayout(offsets, visualLeaderIndex);
            return layout;
        }
    }
}