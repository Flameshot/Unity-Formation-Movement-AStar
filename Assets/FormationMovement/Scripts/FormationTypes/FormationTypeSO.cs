using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    /// <summary>
    /// Defines the method of formation simulation.
    /// </summary>
    public enum SimulationType
    {
        /// <summary>
        /// Uses recorded history data (e.g., buffered position samples) to compute follower movement.
        /// </summary>
        ComputeHistoryData = 0,
        
        /// <summary>
        /// Computes follower movement using the current leader position.
        /// </summary>
        ComputeLiveData    = 1
    }
    
    /// <summary>
    /// Holds formation offset data and the index of the visual leader in the formation.
    /// </summary>
    public struct FormationLayout
    {
        public List<Vector3> Offsets { get; private set; }
        public int VisualLeaderIndex { get; private set; }
       
        public FormationLayout(List<Vector3> offsets, int visualLeaderIndex)
        {
            Offsets = offsets;
            VisualLeaderIndex = visualLeaderIndex;
        }
    }
    
    public abstract class FormationTypeSO :  ScriptableObject
    {
        [field:SerializeField] public SimulationType SimulationType { get; private set; }
        [field: SerializeField] public FormationFollower FollowerPrefab { get; private set; }
        
        /// <summary>
        /// Z offset to apply to the leader in order to position the whole formation behind him.
        /// </summary>
        [field: SerializeField] public float LeaderZOffset { get; private set; }
        [field: SerializeField] public int FollowersCount { get; private set; }
        
        /// <summary>
        /// Spacing between each follower in the formation.
        /// </summary>
        [field: SerializeField] public float DistanceBetweenFollowers { get; private set; }
        [field: SerializeField] public float FormationSpeed { get; private set; }
        
        protected abstract List<Vector3> GetSpawnOffsets(int followersCount, float distanceBetweenFollowers);
        public abstract FormationLayout GetFormationLayout(int followersCount, float distanceBetweenFollowers);
        public FormationLayout GetFormationLayout() => GetFormationLayout(FollowersCount, DistanceBetweenFollowers);
    }
}