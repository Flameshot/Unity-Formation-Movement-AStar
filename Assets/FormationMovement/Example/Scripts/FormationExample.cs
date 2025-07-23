using System.Collections.Generic;
using UnityEngine;

namespace FormationMovement
{
    public class FormationExample : MonoBehaviour
    {
        [SerializeField] private FormationLeader formationLeader;
        [SerializeField] private Color32 leaderColor;
        [SerializeField] private Color32 followersColor;
        [SerializeField] private List<FormationTypeSO> allFormations = new();
        [Space]
        [SerializeField] private List<Transform> waypoints = new();

        private int _waypointIndex;
        private bool _destinationReached;
        private int _formationTypeIndex;
        
        #region Life Cycle
        
        private void Start()
        {
            if (formationLeader == null)
            {
                Debug.LogError("Formation leader is null", this);
                enabled = false;
                return;
            }

            formationLeader.UpdateFormationColor(leaderColor, followersColor);
            _waypointIndex = 0;
            var target = waypoints[_waypointIndex];
            formationLeader.UpdateTarget(target);
        }
        
        private void Update()
        {
            if (formationLeader == null || waypoints == null || waypoints.Count == 0)
                return;

            var formationReachedTarget = formationLeader.HasEntireFormationReachedDestination();
            if (formationReachedTarget && !_destinationReached)
            {
                _destinationReached = true;
                
                _waypointIndex++;
                if (_waypointIndex >= waypoints.Count)
                    _waypointIndex = 0;
                
                formationLeader.UpdateTarget(waypoints[_waypointIndex]);
                return;
            }

            if (!formationReachedTarget && _destinationReached)
                _destinationReached = false;
        }

        #endregion
        
        #region Formation Features

        public void ChangeVisualLeader()
        {
            if(formationLeader == null)
                return;
            
            formationLeader.ChangeVisualLeader();
            formationLeader.UpdateFormationColor(leaderColor, followersColor);
        }

        public void ChangeFormation()
        {
            if (formationLeader == null || allFormations == null || allFormations.Count == 0)
                return;
            
            var randomIndex = Random.Range(0, allFormations.Count);
            var randomFormation = allFormations[randomIndex];
            
            formationLeader.ChangeFormation(randomFormation);
            formationLeader.UpdateFormationColor(leaderColor, followersColor);
        }
        
        public void SwitchToNextFormation()
        {
            if (formationLeader == null || allFormations == null || allFormations.Count == 0)
                return;

            _formationTypeIndex++;
            if (_formationTypeIndex >= allFormations.Count)
                _formationTypeIndex = 0;
            
            var nextFormation = allFormations[_formationTypeIndex];
            
            formationLeader.ChangeFormation(nextFormation);
            formationLeader.UpdateFormationColor(leaderColor, followersColor);
        }

        #endregion
    }
}