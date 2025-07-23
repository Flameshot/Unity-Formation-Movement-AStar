using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    [CreateAssetMenu(fileName = "MatrixFormation", menuName = "Formation Movement/Matrix")]
    public class MatrixFormation : FormationTypeSO
    {
        [Space] 
        [SerializeField] private int columns;
        [SerializeField] private int leaderRow = 1;
        [SerializeField] private bool fill = true;

        protected override List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = new List<Vector3>();

            var halfColumns = (columns - 1) / 2;
            var rowCount = followersCount / columns;
            
            for (var i = 0; i < followersCount; i++)
            {
                var rowIndex = i / columns;
                var columnIndex = i % columns;

                var isLeftMargin = columnIndex == 0;
                if (!fill)
                {
                    var isMarginUp = rowIndex == 0;
                    var isMarginDown = rowIndex == rowCount - 1;
                    var isRightMargin = columnIndex == columns - 1;

                    var isBorder = isMarginUp || isMarginDown || isLeftMargin || isRightMargin;
                    
                    if(!isBorder)
                        continue;
                }

                var offsetX = (columnIndex - halfColumns) * distanceBetweenFollowers;
                var offsetZ = -(rowIndex + 1) * distanceBetweenFollowers;

                var spawnOffset = new Vector3(offsetX, 0f, offsetZ);
                offsets.Add(spawnOffset);
            }

            return offsets;
        }

        public override FormationLayout GetFormationLayout(int followersCount, float distanceBetweenFollowers)
        {
            var offsets = GetSpawnOffsets(followersCount, distanceBetweenFollowers);
            var columnsCount = columns;
            var halfColumns = (columnsCount - 1) / 2;
            var visualLeaderIndexRaw = leaderRow * columnsCount + halfColumns;
            var visualLeaderIndex = Mathf.Clamp(visualLeaderIndexRaw,
                0,
                offsets.Count - 1);

            var layout = new FormationLayout(offsets, visualLeaderIndex);
            return layout;
        }
    }
}