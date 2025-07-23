using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;

namespace FormationMovement
{
    public class Formation : MonoBehaviour
    {
        [Header("Formation Setup")]
        [SerializeField] private FormationTypeSO formationToSpawn;
        [field: SerializeField] public List<FormationGridPoint> FormationGridPoints { get; private set; } = new();

        /// <summary>
        /// Followers in the current formation, including the visual leader.
        /// </summary>
        [field: SerializeField] public List<FormationFollower> Followers { get; private set; }

        /// <summary>
        /// Maximum number of historical entries needed for followers relying on path history.
        /// </summary>
        [field: SerializeField, ReadOnly] public int HistoryEntries { get; private set; }

        private IAstarAI _ai;
        private FormationLeader _formationLeader;
        private float _distanceBetweenFollowers;
        
        /// <summary>
        /// The currently assigned visual leader (not the logical leader).
        /// </summary>
        public FormationFollower VisualLeader { get; private set; }
        
        /// <summary>
        /// The simulation method currently used for moving formation
        /// </summary>
        public SimulationType SimulationType { get; private set; }
        
        /// <summary>
        /// When using Simulation Type = ComputeHistoryData, this is the max capacity allowed for
        /// the circular buffer. Set this to a value taking in consideration followers count and
        /// circular buffer max size for performance reasons.
        /// </summary>
        private const int MaxHistoryEntries = 300;

        #region Life Cycle

        private void Awake()
        {
            _ai = GetComponent<IAstarAI>();
            _formationLeader = GetComponent<FormationLeader>();
        }

        private void Start()
        {
            SpawnFormation(formationToSpawn, true);
        }

        /// <summary>
        /// Instantiates and arranges followers based on the specified formation definition.
        /// </summary>
        public void SpawnFormation(FormationTypeSO formationToSpawn, bool teleport)
        {
            if (_ai == null)
            {
                Debug.LogError("AI component is null", this);
                return;
            }

            if (formationToSpawn == null || formationToSpawn.FollowerPrefab == null)
                return;

            Followers = new List<FormationFollower>();
            FormationGridPoints = new List<FormationGridPoint>();
            _distanceBetweenFollowers = formationToSpawn.DistanceBetweenFollowers;

            AlignLeaderToGround(_ai.position);

            var formationLayout = formationToSpawn.GetFormationLayout();
            var spawnOffsets = formationLayout.Offsets;
            var visualLeaderIndex = formationLayout.VisualLeaderIndex;

            for (var i = 0; i < spawnOffsets.Count; i++)
            {
                var offset = spawnOffsets[i];

                var follower = Instantiate(formationToSpawn.FollowerPrefab, Vector3.zero, Quaternion.identity, null);
                follower.name = "Follower " + i;
                if (i == visualLeaderIndex)
                {
                    VisualLeader = follower;
                    follower.name = "VisualLeader";
                }

                AssignGridPointToFollower(follower, offset);
                if (teleport)
                    TeleportFollowerAtOffset(follower, offset);

                Followers.Add(follower);
            }

            var speed = formationToSpawn.FormationSpeed;

            _ai.maxSpeed = speed;
            Followers.ForEach(x =>
            {
                x.LookAt(_formationLeader.transform.forward);
                x.UpdateSpeed(speed);
            });
            
            SimulationType = formationToSpawn.SimulationType;

            if (SimulationType == SimulationType.ComputeHistoryData)
            {
                HistoryEntries = CalculateMaxHistorySamples(_formationLeader.HistorySampleInterval);
                _formationLeader.Initialize(HistoryEntries);
            }
            
            this.formationToSpawn = formationToSpawn;
        }
        
        /// <summary>
        /// Teleports the formation leader onto the nearest walkable node in the active grid graph.
        /// </summary>
        private void AlignLeaderToGround(Vector3 position)
        {
            var gridGraph = AstarPath.active.graphs.FirstOrDefault(x => x is GridGraph) as GridGraph;
            if (gridGraph == null)
                return;

            var nearestNode = gridGraph.GetNearest(position);
            if (nearestNode.node != null && nearestNode.node.Walkable)
                _ai.Teleport((Vector3)nearestNode.node.position);
        }
        
        /// <summary>
        /// Adds a new grid point tied to a follower using a defined local offset.
        /// </summary>
        private void AssignGridPointToFollower(FormationFollower follower, Vector3 offset)
        {
            var gridPoint = new FormationGridPoint(_formationLeader, offset);
            follower.InitializeGridPoint(gridPoint);
            FormationGridPoints.Add(gridPoint);
        }
        
        /// <summary>
        /// Teleports a follower into position based on its offset relative to the leader.
        /// </summary>
        private void TeleportFollowerAtOffset(FormationFollower follower, Vector3 offset)
        {
            var rotationY = GetAITransform().eulerAngles.y;
            var rotatedOffset = Quaternion.Euler(0f, rotationY, 0f) * offset;
            var finalPosition = _ai.position + rotatedOffset;

            follower.Teleport(finalPosition);
        }
        
        /// <summary>
        /// Computes the maximum number of historical position samples needed
        /// based on follower offsets and sampling interval.
        /// </summary>
        private int CalculateMaxHistorySamples(float sampleInterval)
        {
            var minExpectedVelocity = Mathf.Max(_ai.maxSpeed * 0.1f, 0.01f);
            var maxOffset = FormationGridPoints.Max(x => x.GridPointOffset.magnitude);
            var maxTime = maxOffset / minExpectedVelocity;
            var needed = Mathf.CeilToInt(maxTime / sampleInterval);

            var maxHistoryEntries = Mathf.Clamp(needed, Followers.Count, MaxHistoryEntries);
            return maxHistoryEntries;
        }

        private Transform GetAITransform()
        {
            Transform transform = null; 
            switch (_ai)
            {
                case AIPath aiPath:
                    transform = aiPath.transform;
                    break;
                case AILerp aiLerp:
                    transform = aiLerp.transform;
                    break;
                /*
                case RichAI richAi:
                    transform = richAI.transform;
                    break;
                case FollowerEntity entity:
                    transform = entity.transform;
                    break;*/
                default:
                    Debug.Log("GetAITransform was not implemented for this type: " + _ai, this);
                    break;
            }

            return transform;
        }
        
        public void ResetAllGridPoints()
        {
            FormationGridPoints.ForEach(x => x.Reset());
        }

        /// <summary>
        /// Initializes grid points based on available path distance.
        /// Returns the number of grid points unable to use historical data.
        /// </summary>
        public int InitializeGridPoints(float distance)
        {
            var gridsWithMissingData = 0;

            foreach (var gridPoint in FormationGridPoints)
            {
                var gridOffset = gridPoint.GridPointOffset;
                var zOffset = Mathf.Abs(gridOffset.z);
                var hasHistoryData = zOffset <= distance;
                if(!hasHistoryData)
                    gridsWithMissingData++;

                gridPoint.hasHistoryData = hasHistoryData;
            }

            return gridsWithMissingData;
        }

        #endregion

        #region Formation Movement

        /// <summary>
        /// Updates follower positions during history-driven movement simulation.
        /// </summary>
        public void UpdateFormationHistoryPositions(MovementState leaderState)
        {
            foreach (var gridPoint in FormationGridPoints)
            {
                var gridPointReached = gridPoint.reached;
                var hasHistoryData = gridPoint.hasHistoryData;
                var historyDataInUse = gridPoint.IsUsingHistoryData;

                if (leaderState == MovementState.Moving && !gridPointReached)
                    gridPoint.UpdatePositionFromHistory();
                else
                if (leaderState == MovementState.ReachedDestination)
                {
                    if(gridPointReached)
                        continue;
                    
                    // GridPoint/Follower needs fake history path data
                    if(!hasHistoryData && !historyDataInUse)
                        gridPoint.UpdateFakeHistoryPosition();
                    else
                    // GridPoint/Follower has data, but didn't got the time to start path
                    if (hasHistoryData && !historyDataInUse)
                        gridPoint.UpdatePositionFromFormationTail();
                }
            }
        }

        /// <summary>
        /// Updates follower positions using live simulation (ignores history).
        /// </summary>
        public void UpdateFormationLivePositions(MovementState leaderState)
        {
            foreach (var gridPoint in FormationGridPoints)
            {
                var gridPointReached = gridPoint.reached;

                switch (leaderState)
                {
                    case MovementState.Moving:
                    case MovementState.ReachedDestination:
                        if(!gridPointReached)
                            gridPoint.UpdateLivePosition();
                        break;
                }
            }
        }

        #endregion

        #region Formation Management
        
        /// <summary>
        /// Randomly selects a new visual leader from existing followers
        /// and updates their assigned offsets and grid points accordingly.
        /// </summary>
        public void RandomizeVisualLeader()
        {
            var formationLayout = formationToSpawn.GetFormationLayout();
            var visualLeaderIndex = formationLayout.VisualLeaderIndex;

            // pick a random follower as new visual leader
            var newLeaderIndex = UnityEngine.Random.Range(0, Followers.Count);
            var newVisualLeader = Followers[newLeaderIndex];

            // find who is currently in the visual leader spot
            var currentLeaderAtSlot = Followers[visualLeaderIndex];

            // swap positions
            Followers[visualLeaderIndex] = newVisualLeader;
            Followers[newLeaderIndex] = currentLeaderAtSlot;
            VisualLeader = newVisualLeader;
            
            for (var i = 0; i < FormationGridPoints.Count; i++)
            {
                var gridPoint = FormationGridPoints[i];
                var follower = Followers[i];
                follower.InitializeGridPoint(gridPoint);
            }
        }

        public void ChangeFormation(FormationTypeSO formationType)
        {
            var formationLayout = formationType.GetFormationLayout(Followers.Count, _distanceBetweenFollowers);
            var spawnOffsets = formationLayout.Offsets;
            var visualLeaderIndex = formationLayout.VisualLeaderIndex;
            
            FormationGridPoints = new List<FormationGridPoint>();
            for (var i = 0; i < spawnOffsets.Count; i++)
            {
                var offset = spawnOffsets[i];
                var follower = Followers[i];
                if (i == visualLeaderIndex)
                    VisualLeader = follower;
                    
                AssignGridPointToFollower(follower, offset);
            }
            
            HistoryEntries = CalculateMaxHistorySamples(_formationLeader.HistorySampleInterval);
            _formationLeader.Initialize(HistoryEntries);

            formationToSpawn = formationType;
        }
        
        public void ApplyFollowerColors(Color32 leaderColor, Color32 followerColor)
        {
            foreach (var follower in Followers)
            {
                SetColor(follower, followerColor);
            }
            
            SetColor(VisualLeader, leaderColor);
        }

        private void SetColor(FormationFollower follower, Color32 color)
        {
            if(follower == null)
                return;
            
            var renderers = follower.GetComponentsInChildren<MeshRenderer>();
            if(renderers == null)
                return;
        
            foreach (var meshRender in renderers)
            {
                meshRender.material.SetColor("_BaseColor", color);
            }
        }

        #endregion
    }
}