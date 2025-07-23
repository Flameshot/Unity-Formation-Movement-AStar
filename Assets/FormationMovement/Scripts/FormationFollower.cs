using Pathfinding;
using UnityEngine;

namespace FormationMovement
{
    public enum MovementState
    {
        None               = 0,
        Idle               = 1,
        Moving             = 2,
        ReachedDestination = 3
    }
    
    [RequireComponent(typeof(AIPath))]
    public class FormationFollower : FormationAI
    {
        [SerializeField] private float maximumVelocityCoefficient = 1.5f;
        [SerializeField][ReadOnly] private FormationGridPoint gridPoint;
        
        protected override void Awake()
        {
            base.Awake();
            ai.canSearch = false;
        }

        protected override void FixedUpdate()
        {
            if (ai == null || gridPoint == null)
                return;

            var targetPosition = gridPoint.Position;
            VelocityMagnitude = new Vector3(ai.desiredVelocity.x, 0f, ai.desiredVelocity.z).magnitude;

            switch (State)
            {
                case MovementState.Idle:
                    var shouldChaseTarget = ShouldChaseTarget(targetPosition, 0.1f);
                    if (!shouldChaseTarget)
                        return;

                    SetState(MovementState.Moving);
                    break;
                case MovementState.Moving:
                    UpdateSpeedBasedOnGridGap();
                    
                    ai.destination = targetPosition;
                    if (!ai.pathPending)
                        ai.SearchPath();
                    
                    if(!ai.reachedDestination && !ai.reachedEndOfPath)
                        return;
                
                    if (VelocityMagnitude > 0f)
                        return;

                    var isLeaderStopped = gridPoint.IsLeaderStopped();
                    if(!isLeaderStopped)
                        return;
                    
                    var leaderDirection = gridPoint.LeaderForward;
                    if (!IsFacingDot(leaderDirection))
                    {
                        HandleRotation(leaderDirection);
                        return;
                    }
                
                    SetState(MovementState.ReachedDestination);
                    break;
                case MovementState.ReachedDestination:
                    shouldChaseTarget = ShouldChaseTarget(targetPosition, 0.1f);
                    if (!shouldChaseTarget)
                        return;

                    SetState(MovementState.Moving);
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
                    EnableRotation(true);
                    break;
                case MovementState.ReachedDestination:
                    ai.isStopped = true;
                    gridPoint.reached = true;
                    ai.SetPath(null);
                    ai.destination = Vector3.positiveInfinity;
                    break;
            }
        
            State = state;
        }
        
        public void Teleport(Vector3 position)
        {
            if(ai == null)
                return;
        
            ai.Teleport(position);
        }

        public void InitializeGridPoint(FormationGridPoint formationGridPoint)
        {
            gridPoint = formationGridPoint;
            SetState(MovementState.Idle);
        }

        private void UpdateSpeedBasedOnGridGap()
        {
            if (!ai.hasPath)
                return;

            var gridLivePosition = gridPoint.GetLivePosition();

            // speed up based on how far behind we are from where we should be in formation
            var distanceToFormationPosition = Vector3.Distance(ai.position, gridLivePosition);

            // ideal distance to maintain
            var idealGap = GetEndReachedDistance() - 0.1f;
            // max gap before speed increase
            var maxGap = distanceToFormationPosition - 1f;

            if (distanceToFormationPosition > idealGap)
            {
                var gapRatio = Mathf.InverseLerp(idealGap, maxGap, distanceToFormationPosition);
                ai.maxSpeed = Mathf.Lerp(defaultSpeed, defaultSpeed * maximumVelocityCoefficient, gapRatio);
            }
            else
                ai.maxSpeed = defaultSpeed;
        }

        public void LookAt(Vector3 direction)
        {
            if(ai == null)
                return;
        
            GetAITransform().rotation = Quaternion.LookRotation(direction);
        }
        
        public void UpdateSpeed(float maxSpeed)
        {
            if(ai == null)
                return;
        
            ai.maxSpeed = maxSpeed;
            defaultSpeed = maxSpeed;
        }
    }
}