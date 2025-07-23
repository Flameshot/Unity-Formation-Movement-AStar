using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "TriangleFormation", menuName = "Formation Movement/Triangle")]
    public class TriangleFormationSO: FormationTypeSO
    {
        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();
            
            // place visual leader at tip (always first)
            offsets.Add(Vector3.zero - new Vector3(0f, 0f, LeaderZOffset));
            var unitsPlaced = 1;

            for (var row = 1; unitsPlaced < followersCount; row++)
            {
                var unitsThisRow = row + 1;
                var z = -row * distanceBetweenFollowers;

                for (var i = 0; i < unitsThisRow; i++)
                {
                    // spread across the row
                    var totalWidth = (unitsThisRow - 1) * distanceBetweenFollowers;
                    var x = -totalWidth / 2f + i * distanceBetweenFollowers;

                    offsets.Add(new Vector3(x, 0f, z - LeaderZOffset));
                    unitsPlaced++;

                    if (unitsPlaced >= followersCount)
                        break;
                }
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