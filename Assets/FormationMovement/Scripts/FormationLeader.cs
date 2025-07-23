using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

namespace FormationMovement
{
    [RequireComponent(typeof(Formation), typeof(AIDestinationSetter), typeof(AIPath))]
    public class FormationLeader : FormationAI
    {
        [field: SerializeField, ReadOnly] public float VelocityAverageOverTime { get; private set; }
        [field: SerializeField] public float HistorySampleInterval { get; private set; } = 0.2f;

        private AIDestinationSetter _destinationSetter;
        private Formation _formation;
        private CircularBuffer<FormationLocation> _formationHistory;
        private CircularBuffer<FormationLocation> _fakeFormationHistory;
        private int _maxHistoryEntries;
        private float _lastSampleTime;

        private const int MinHistorySamplesNeeded = 1;

        /// <summary>
        /// The last follower in formation that has historical data (HasHistoryData)
        /// and uses it to move (HistoryDataInUse)
        ///
        /// In some cases it can be the leader himself
        /// </summary>
        public FormationAI FormationTail { get; private set; }

        /// <summary>
        /// Grid point corresponding to FormationTail
        ///
        /// When leader is FormationTail, this is null
        /// </summary>
        public FormationGridPoint TailGrid { get; private set; }
        
        public float MaxSpeed => ai.maxSpeed;

        // Data needed for sampling "fake" history
        private Vector3 _simulationStartPos;
        private Vector3 _simulationFinalPos;
        private float _simulationStartRotY;
        private float _simulationTargetRotY;
        private bool _simulated;
        private FormationGridPoint _lastGridWithPath;

        #region Life Cycle

        protected override void Awake()
        {
            base.Awake();
            _formation = GetComponent<Formation>();
            _destinationSetter = GetComponent<AIDestinationSetter>();
        }

        protected override void FixedUpdate()
        {
            if (ai == null || _formation == null || _destinationSetter == null || _destinationSetter.target == null)
                return;

            UpdateFormationMovement();

            var targetPosition = _destinationSetter.target.position;
            switch (State)
            {
                case MovementState.None:
                case MovementState.Idle:
                case MovementState.ReachedDestination:
                    if (!ai.canMove)
                        return;

                    var shouldChaseTarget = ShouldChaseTarget(targetPosition, 1f);
                    if (!shouldChaseTarget)
                        return;

                    SetState(MovementState.Moving);
                    break;
                case MovementState.Moving:
                    if (ai.isStopped)
                        return;

                    ai.destination = targetPosition;
                    if (!ai.pathPending)
                        ai.SearchPath();

                    if (!ai.reachedDestination)
                        return;

                    if (VelocityMagnitude > 0f)
                        return;

                    SetState(MovementState.ReachedDestination);
                    break;
            }
        }

        public override void SetState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Idle:
                    break;
                case MovementState.Moving:
                    ai.isStopped = false;
                    _formation.ResetAllGridPoints();
                    if (_formation.SimulationType == SimulationType.ComputeHistoryData)
                        StartCoroutine(InitializeDestination(InitializeHistory));
                    break;
                case MovementState.ReachedDestination:
                    ai.isStopped = true;
                    VelocityAverageOverTime = 0f;
                    VelocityMagnitude = 0f;
                    _lastSampleTime = Time.fixedTime;
                    SampleFakeHistory();
                    UpdateFormationTail();
                    //Debug.Log("[Formation Leader] Reached destination");
                    break;
            }

            State = state;
        }

        /// <summary>
        /// Find and set the last grid and follower who has a path and is walking on it.
        /// </summary>
        private void UpdateFormationTail()
        {
            FormationTail = null;
            TailGrid = null;
            var gridPoints = _formation.FormationGridPoints;
            for (var i = gridPoints.Count - 1; i >= 0; i--)
            {
                var grid = gridPoints[i];
                if (grid.hasHistoryData && grid.IsUsingHistoryData)
                {
                    FormationTail = _formation.Followers[i];
                    TailGrid = grid;
                    Debug.Log("[Formation Leader] Use grid with index " + i + " as formation tail");
                    break;
                }
            }

            if (FormationTail != null) 
                return;
            
            FormationTail = this;
            Debug.Log("[Formation Leader] Use leader as formation tail");
        }

        public void UpdateTarget(Transform target)
        {
            if (_destinationSetter == null)
                return;

            _destinationSetter.target = target;
        }

        public bool HasEntireFormationReachedDestination()
        {
            if (_formation == null)
                return false;

            var targetsReached = 0;

            var gridPoints = _formation.FormationGridPoints;
            foreach (var gridPoint in gridPoints)
            {
                if (!gridPoint.reached)
                    continue;

                targetsReached++;
            }

            return targetsReached == gridPoints.Count;
        }

        #endregion

        #region Formation Helpers

        public void CreateFormation(FormationTypeSO formationToSpawn, bool teleport)
        {
            if (_formation == null)
                return;

            _formation.SpawnFormation(formationToSpawn, teleport);
        }

        public void ChangeVisualLeader()
        {
            if (_formation == null)
                return;

            _formation.RandomizeVisualLeader();
        }

        public void ChangeFormation(FormationTypeSO formationType)
        {
            if (_formation == null || formationType == null)
                return;

            _formation.ChangeFormation(formationType);
        }

        public void UpdateFormationColor(Color32 leaderColor, Color32 followerColor)
        {
            if (_formation == null)
                return;

            _formation.ApplyFollowerColors(leaderColor, followerColor);
        }

        #endregion

        #region Formation Movement

        /// <summary>
        /// Updates formation movement based on the current simulation type.
        /// Handles velocity sampling and formation grid updates.
        /// </summary>
        private void UpdateFormationMovement()
        {
            var simulationType = _formation.SimulationType;
            switch (simulationType)
            {
                case SimulationType.ComputeHistoryData:
                    if (_formationHistory == null)
                        return;

                    ComputeAverageVelocity();

                    if (State == MovementState.Moving)
                    {
                        var shouldSamplePosition = Time.fixedTime - _lastSampleTime >= HistorySampleInterval;
                        if (shouldSamplePosition)
                            SampleCurrentPosition();
                    }
                    
                    _formation.UpdateFormationHistoryPositions(State);
                    break;
                case SimulationType.ComputeLiveData:
                    _formation.UpdateFormationLivePositions(State);
                    break;
            }
        }

        private void ComputeAverageVelocity()
        {
            VelocityMagnitude = new Vector3(ai.desiredVelocity.x, 0f, ai.desiredVelocity.z).magnitude;
            VelocityAverageOverTime = 0.0f;

            if (State != MovementState.Moving)
                return;

            if (_formationHistory.Count > MinHistorySamplesNeeded)
            {
                var firstLocation = _formationHistory.GetOldest();

                VelocityAverageOverTime = (ai.position - firstLocation.position).magnitude /
                                          (Time.fixedTime - firstLocation.time);
            }
            else
                VelocityAverageOverTime = 0.0f;
        }

        /// <summary>
        /// Records the current position, rotation, and time into the formation's history buffer.
        /// Called at fixed sampling intervals.
        /// </summary>
        private void SampleCurrentPosition()
        {
            _lastSampleTime = Time.fixedTime;

            _formationHistory.Add(new FormationLocation
            {
                position = ai.position,
                time = Time.fixedTime,
                rotationY = ai.rotation.eulerAngles.y
            });
        }

        /// <summary>
        /// Initializes follower grid points once the leader has computed a valid path distance.
        /// Note: Read more at GetRemainingDistance() definition.
        /// </summary>
        private IEnumerator InitializeDestination(Action onFinish)
        {
            var totalPathDistance = -1f;
            var maxAttempts = 3;
            var attempts = 0;

            var targetPosition = _destinationSetter.target.position;

            while (attempts < maxAttempts)
            {
                totalPathDistance = GetRemainingDistance(targetPosition);

                if (totalPathDistance > 0f)
                    break;

                attempts++;
                yield return null;
            }

            if (totalPathDistance > 0f)
            {
                var gridsWithMissingData = _formation.InitializeGridPoints(totalPathDistance);
                var shouldComputeFakePath = gridsWithMissingData > 0;
                if (shouldComputeFakePath)
                {
                    var lastWithPathIndex = _formation.Followers.Count - gridsWithMissingData - 1;
                    
                    _lastGridWithPath = _formation.FormationGridPoints[lastWithPathIndex];
                    ComputeFakePath();

                    Debug.LogError("Last with path index:" + lastWithPathIndex);
                    Debug.Log("[Formation Leader] " + gridsWithMissingData + " followers need fake path");
                    //Debug.Log("[Formation Leader] Predicted based on path distance: " + totalPathDistance);
                }
                else
                    Debug.Log("[Formation Leader] All followers will use history data!");
            }
            else
                Debug.LogError("[Formation Leader] Could not get path distance after multiple attempts!");

            onFinish?.Invoke();
        }

        /// <summary>
        /// Tries to find an interpolated position from the formation's history buffer for grid points to use.
        /// </summary>
        public bool TryGetInterpolatedPositionAtTime(float targetTime, out FormationLocation result,
            bool fakeHistory = false)
        {
            if (fakeHistory)
                return _fakeFormationHistory.TryInterpolate(targetTime, out result);

            return _formationHistory.TryInterpolate(targetTime, out result);
        }

        public void Initialize(int maxHistoryEntries)
        {
            _maxHistoryEntries = maxHistoryEntries;
            InitializeHistory();
            Debug.Log("[Formation Leader] History Max Entries: " + maxHistoryEntries);
        }

        private void InitializeHistory()
        {
            _formationHistory = new CircularBuffer<FormationLocation>(_maxHistoryEntries);
            _lastSampleTime = Time.fixedTime;
        }

        #endregion

        #region Adaptive Positioning Fallback
        
         /* Below implementation tries to fix an edge case that happens on most formation layouts, but most
         *  visible especially on VerticalFormation.
         *
         * This case happens when the length of the path (from the leader to its destination)
         * is lower than the farthest GridPoint/Follower grid offset in the formation, resulting in
         * not finding sampled data for some GridPoints/Followers.
         * 
         * When "FormationLeader" reaches its destination, a fake path along "_lastGridWithPath" and "FormationLeader"
         * is sampled in "_fakeFormationHistory" buffer for GridPoint/Follower to use.
         *
         * This gives a good result sometimes, although improvements are needed in below methods. Instead
         * of populating "_fakeFormationHistory" with a path made from a straight line,
         * (from "_simulationStartPos" to "_simulationFinalPos"), we could find/use the data
         * from "_formationHistory" in order to get a better path
         */
        
        /// <summary>
        /// Calculates the path from last follower with path position to leader's destination
        /// </summary>
        private void ComputeFakePath()
        {
            // Get the leader's destination position
            var leaderDestination = _destinationSetter.target.position;

            // Get the offset of the last follower with path relative to leader
            var followerOffset = _lastGridWithPath.GridPointOffset;

            // Calculate where the last follower will be when leader reaches destination
            // You might need to rotate the offset based on leader's destination direction
            var leaderDirection = (leaderDestination - ai.position).normalized;
            var rotatedOffset = RotateOffsetToDirection(followerOffset, leaderDirection);

            _simulationStartPos = leaderDestination + rotatedOffset;
            _simulationFinalPos = leaderDestination;

            _simulationStartRotY = ai.rotation.eulerAngles.y;
            _simulationTargetRotY = Quaternion.LookRotation(_simulationFinalPos - _simulationStartPos).eulerAngles.y;

#if UNITY_EDITOR
            Debug.DrawLine(_simulationStartPos, _simulationFinalPos, Color.magenta, 10f);
#endif
            _simulated = false;
        }
        
        /// <summary>
        /// Sample movement history along the computed fake path
        /// </summary>
        private void SampleFakeHistory()
        {
            if (_simulated)
                return;

            _fakeFormationHistory = new CircularBuffer<FormationLocation>(_maxHistoryEntries);

            var distance = Vector3.Distance(_simulationStartPos, _simulationFinalPos);
            var speed = MaxSpeed;
            var duration = Mathf.Max(distance / speed, Time.fixedDeltaTime);

            var sampleInterval = Time.fixedDeltaTime;
            var simulationStartTime = Time.fixedTime - duration - sampleInterval;
            var currentTime = 0f;

            while (currentTime <= duration + sampleInterval)
            {
                var t = Mathf.Clamp01(currentTime / duration);

                var simulatedPosition = Vector3.Lerp(_simulationStartPos, _simulationFinalPos, t);
                var forward = (_simulationFinalPos - _simulationStartPos).normalized;
                var simulatedRotY = Quaternion.LookRotation(forward).eulerAngles.y;

                _fakeFormationHistory.Add(new FormationLocation
                {
                    position = simulatedPosition,
                    time = simulationStartTime + currentTime,
                    rotationY = simulatedRotY
                });
#if  UNITY_EDITOR
                Debug.DrawLine(simulatedPosition, simulatedPosition + Vector3.up * 1.5f, Color.red, 5f);
#endif
                currentTime += sampleInterval;
            }

            _fakeFormationHistory.Add(new FormationLocation
            {
                position = _simulationFinalPos,
                time = simulationStartTime + duration + sampleInterval,
                rotationY = _simulationTargetRotY
            });

            _simulated = true;
        }

        private Vector3 RotateOffsetToDirection(Vector3 offset, Vector3 direction)
        {
            var angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, angle, 0);
            return rotation * offset;
        }

        #endregion
    }
}