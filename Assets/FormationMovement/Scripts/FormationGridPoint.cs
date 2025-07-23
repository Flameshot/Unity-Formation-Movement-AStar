using System;
using UnityEngine;

namespace FormationMovement
{
    [Serializable]
    public class FormationLocation
    {
        public Vector3 position;
        public float time;
        public float rotationY;
    }
    
    [Serializable]
    public class FormationGridPoint
    {
        [field:SerializeField] public Vector3 GridPointOffset { get; private set; }
        [field:SerializeField] public Vector3 Position { get; private set; }
        [field:SerializeField] public bool IsUsingHistoryData { get; private set; }
        [field:SerializeField] public bool reached;
        [field:SerializeField] public bool hasHistoryData;

        public FormationLeader FormationLeader { get; private set; }
        public Vector3 LeaderForward => FormationLeader.transform.forward;
        
        public FormationGridPoint(FormationLeader formationLeader, Vector3 offset)
        {
            FormationLeader = formationLeader;
            GridPointOffset = offset;
            UpdateLivePosition();
        }
        
        /// <summary>
        /// Updates position relative to current leader position/rotation
        /// </summary>
        public void UpdateLivePosition()
        {
            var rotationY = FormationLeader.transform.eulerAngles.y;
            var rotatedPositionOffset = Quaternion.Euler(0, rotationY, 0) * new Vector3(GridPointOffset.x, 0, GridPointOffset.z);
            Position = FormationLeader.Position + rotatedPositionOffset;
        }
        
        /// <summary>
        /// Returns live position relative to current leader
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLivePosition()
        {
            var rotationY = FormationLeader.transform.eulerAngles.y;
            var rotatedPositionOffset = Quaternion.Euler(0, rotationY, 0) * new Vector3(GridPointOffset.x, 0, GridPointOffset.z);
            return FormationLeader.Position + rotatedPositionOffset;
        }

        /// <summary>
        /// Updates position using leader's historical movement data
        /// </summary>
        public void UpdatePositionFromHistory()
        {
            var averageVelocity = FormationLeader.VelocityAverageOverTime;
            if(averageVelocity <= 0f)
                return;

            var deltaTime = -GridPointOffset.z / averageVelocity;
            var targetTime = Time.fixedTime - deltaTime;

            var foundHistoryData =
                FormationLeader.TryGetInterpolatedPositionAtTime(targetTime, out var leaderLocation);
            if(!foundHistoryData)
                return;

            Position = leaderLocation.position;
            if (Mathf.Abs(GridPointOffset.x) > 0f)
            {
                var forward = (FormationLeader.Position - leaderLocation.position).normalized;
                var right = GetRightVectorXZ(forward);
                Position += right * GridPointOffset.x;
            }

            IsUsingHistoryData = true;
        }

        /// <summary>
        /// Updates position using predicted path when no history data is available
        /// </summary>
        public void UpdateFakeHistoryPosition()
        {
            var averageVelocity = FormationLeader.MaxSpeed;
            var deltaTime = -GridPointOffset.z / averageVelocity;
            var targetTime = Time.fixedTime - deltaTime;
            
            var foundFakeHistoryData =
                FormationLeader.TryGetInterpolatedPositionAtTime(targetTime, out var leaderLocation, true);
            if(!foundFakeHistoryData)
                return;

            var forward = Quaternion.Euler(0f, leaderLocation.rotationY, 0f) * Vector3.forward;
            
            var right = GetRightVectorXZ(forward);
            var offset = right * GridPointOffset.x + forward * GridPointOffset.z;
            
            Position = leaderLocation.position + offset;
        }

        /// <summary>
        /// Updates position relative to the formation tail (last follower with history)
        /// </summary>
        public void UpdatePositionFromFormationTail()
        {
            var formationTail = FormationLeader.FormationTail;
            if (formationTail == null)
                return;

            switch (formationTail)
            {
                case FormationFollower follower:
                    var tailGrid = FormationLeader.TailGrid;
                    if (tailGrid.Position == Vector3.positiveInfinity)
                        return;

                    var averageVelocity = follower.VelocityMagnitude;
                    if (averageVelocity <= 0f)
                        averageVelocity = FormationLeader.MaxSpeed;

                    var deltaTime = -GridPointOffset.z / averageVelocity;
                    var targetTime = Time.fixedTime - deltaTime;
                    
                    var foundHistoryData =
                        FormationLeader.TryGetInterpolatedPositionAtTime(targetTime, out var leaderLocation, false);
                    if (!foundHistoryData)
                        return;

                    var gridOffsetZ = GridPointOffset.z - tailGrid.GridPointOffset.z;
                    var forward = Quaternion.Euler(0f, leaderLocation.rotationY, 0f) * Vector3.forward;

                    var right = GetRightVectorXZ(forward);
                    var offset = right * GridPointOffset.x + forward * gridOffsetZ;

                    Position = tailGrid.Position + offset;
                    break;
                case FormationLeader leader:
                    UpdateFakeHistoryPosition();
                    break;
            }
        }

        public void Reset()
        {
            IsUsingHistoryData = false;
            hasHistoryData = false;
            reached = false;
            Position = Vector3.positiveInfinity;
        }

        public bool IsLeaderStopped()
        {
            return FormationLeader.VelocityAverageOverTime <= 0f;
        }
        
        /// <summary>
        /// Rotates a direction vector 90 degrees clockwise on the XZ plane to get the right vector
        /// </summary>
        public Vector3 GetRightVectorXZ(Vector3 direction)
        {
            return new Vector3(direction.z, 0f, -direction.x);
        }
    }
}