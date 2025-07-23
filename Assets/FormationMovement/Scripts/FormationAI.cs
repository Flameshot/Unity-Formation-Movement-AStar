using Pathfinding;
using UnityEngine;

namespace FormationMovement
{
    public abstract class FormationAI : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public MovementState State { get; protected set; }
        [field: SerializeField, ReadOnly] public float VelocityMagnitude { get; protected set; }
        
        protected IAstarAI ai;
        protected float defaultSpeed;
        
        public Vector3 Position => ai.position;
        
        protected virtual void Awake()
        {
            ai = GetComponent<IAstarAI>();
        }
        
        protected abstract void FixedUpdate();

        public abstract void SetState(MovementState state);
        
        protected bool ShouldChaseTarget(Vector3 position, float chaseDistance)
        {
            if (position == Vector3.positiveInfinity)
                return false;
            
            var distance = GetRemainingDistance(position);

            if (distance <= 0f)
                return false;

            var endReachedDistance = GetEndReachedDistance();
            return distance > chaseDistance + endReachedDistance;
        }
        
        /// <summary>
        /// Computes IAstarAI remaining distance while it doesn't move (isStopped = true).
        /// Usually it takes 1-2 frames.
        ///
        /// Note: https://forum.arongranberg.com/t/best-practice-for-player-movement/13228/8?u=flameshot
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        protected float GetRemainingDistance(Vector3 position)
        {
            ai.destination = position;

            var tmp = ai.isStopped;
            ai.isStopped = false;

            if(!ai.pathPending)
                ai.SearchPath();

            ai.MovementUpdate(0, out var nextPosition, out var nextRotation);
            var remainingDistance = ai.remainingDistance;
            ai.isStopped = tmp;

            if (double.IsInfinity(remainingDistance))
                remainingDistance = -1f;

            return remainingDistance;
        }
        
        protected void HandleRotation(Vector3 targetDirection)
        {
            EnableRotation(false);

            var lookRotation = Quaternion.LookRotation(targetDirection);
            ai.rotation = Quaternion.Slerp(ai.rotation, lookRotation, 10 * Time.fixedDeltaTime);
        }
        
        protected bool IsFacingDot(Vector3 targetDirection, float threshold = 0.99f)
        {
            if (ai == null)
                return false;

            var currentForward = GetAITransform().forward;
            currentForward.y = 0f;
            currentForward.Normalize();

            targetDirection.y = 0f;
            targetDirection.Normalize();

            var dot = Vector3.Dot(currentForward, targetDirection);
            return dot >= threshold;
        }

        protected void EnableRotation(bool enable)
        {
            switch (ai)
            {
                case AIPath aiPath:
                    aiPath.enableRotation = enable;
                    break;
                case AILerp aiLerp:
                    aiLerp.enableRotation = enable;
                    break;
                /*case RichAI richAi:
                    richAi.enableRotation = enable;
                    break;
                case FollowerEntity entity:
                    entity.enableRotation = enable;
                    break;*/
                default:
                    Debug.Log("EnableRotation was not implemented for this type: " + ai, this);
                    break;
            }
        }

        protected float GetEndReachedDistance()
        {
            var endReachedDistance = 0f;
            switch (ai)
            {
                case AIPath aiPath:
                    endReachedDistance = aiPath.endReachedDistance;
                    break;
                /*case AILerp aiLerp:
                    endReachedDistance = aiLerp.endReachedDistance;
                    break;
                case RichAI richAi:
                    endReachedDistance = richAi.endReachedDistance;
                    break;
                case FollowerEntity entity:
                    endReachedDistance = entity.endReachedDistance;
                    break;*/
                default:
                    Debug.Log("GetEndReachedDistance was not implemented for this type: " + ai, this);
                    break;
            }

            return endReachedDistance;
        }
        
        protected Transform GetAITransform()
        {
            Transform transform = null; 
            switch (ai)
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
                    Debug.Log("GetAITransform was not implemented for this type: " + ai, this);
                    break;
            }

            return transform;
        }
    }
}