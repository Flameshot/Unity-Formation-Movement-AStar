using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "WedgeFormation", menuName = "Formation Movement/Wedge")]
    public class WedgeFormationSO : FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();

            // place visual leader at tip (always first)
            offsets.Add(Vector3.zero - new Vector3(0f, 0f, LeaderZOffset));

            var followersLeft = followersCount - 1;

            for (var i = 0; i < followersLeft; i++)
            {
                var pairIndex = (i / 2) + 1;        // 1, 1, 2, 2, 3, 3, etc.
                var side = (i % 2 == 0) ? -1 : 1;   // Left, Right, Left, Right, etc.

                var x = side * pairIndex * distanceBetweenFollowers;
                var z = -pairIndex * distanceBetweenFollowers;

                offsets.Add(new Vector3(x, 0f, z - LeaderZOffset));
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