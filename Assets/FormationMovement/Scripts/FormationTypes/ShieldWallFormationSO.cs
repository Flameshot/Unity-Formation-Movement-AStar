using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "ShieldWallFormation", menuName = "Formation Movement/Shield Wall")]
    public class ShieldWallFormationSO : FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();

            var restCount = followersCount - 1;
            var half = restCount / 2;

            for (var i = 0; i < restCount; i++)
            {
                var xOffset = (i - half) * distanceBetweenFollowers;

                // if even number of followers units, shift to center properly
                if (restCount % 2 == 0)
                    xOffset += distanceBetweenFollowers / 2f;

                var offset = new Vector3(xOffset, 0f, -distanceBetweenFollowers);
                offsets.Add(offset);
            }
            
            var lastFollowerZ   = offsets[restCount - 1].z; 
            var leaderOffsetZ  = lastFollowerZ - distanceBetweenFollowers;
            
            // place visual leader at center back
            offsets.Add(Vector3.forward * leaderOffsetZ); 

            return offsets;
        }

        public override FormationLayout GetFormationLayout(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = GetSpawnOffsets(followersCount, distanceBetweenFollowers);
            var visualLeaderIndex = offsets.Count - 1;
            var layout = new FormationLayout(offsets, visualLeaderIndex);
            return layout;
        }
    }
}