using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "HorizontalLineFormation", menuName = "Formation Movement/Horizontal Line")]
    public class HorizontalFormation : FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();
            
            // Center-aligned horizontal line, followers spaced left to right
            var half = followersCount / 2;
            
            for (var i = 0; i < followersCount; i++)
            {
                // Offset followers from center
                var xOffset = (i - half) * distanceBetweenFollowers;
                
                // If even number of followers, nudge right side a bit to balance the line
                if (followersCount % 2 == 0)
                    xOffset += distanceBetweenFollowers / 2f;
                
                var offset = new Vector3(xOffset, 0f, -LeaderZOffset);
                
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