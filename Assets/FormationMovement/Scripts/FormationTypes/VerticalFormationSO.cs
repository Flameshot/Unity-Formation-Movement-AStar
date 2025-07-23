using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "VerticalLineFormation", menuName = "Formation Movement/Vertical Line")]
    public class VerticalFormationSO : FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();

            for (var i = 0; i < followersCount; i++)
            {
                var offset = new Vector3(0f, 0f, -1f * distanceBetweenFollowers * (i + 1));
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