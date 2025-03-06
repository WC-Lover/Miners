using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Unit
{
    public class UnitState : MonoBehaviour
    {
        public enum State { Searching, Moving, Interacting, Idle }

        private Unit unit;
        private State currentState = State.Idle;
        private InteractionTarget interactionTarget;

        private void Awake()
        {
            unit = GetComponent<Unit>();

            unit.DelegateManager.OnUnitSetUp += DelegateManager_OnUnitSetUp;
        }

        private void Start()
        {
        }

        private void DelegateManager_OnUnitSetUp()
        {
            // Start searching only after Unit has set its' Config
            currentState = State.Searching;

            unit.DelegateManager.OnUnitSetUp -= DelegateManager_OnUnitSetUp;
        }

        private void Update()
        {
            if (!unit.IsOwner) return;

            Debug.Log($"current state -> {currentState}");
            switch (currentState)
            {
                case State.Searching:
                    Search();
                    break;

                case State.Moving:
                    Move();
                    break;

                case State.Interacting:
                    Interact();
                    break;
                
                case State.Idle:
                    break;
            }
        }

        private void Search()
        {
            if (!unit.HasArrivedAtFirstDestination)
            {
                // Target is first destination set by player.
                this.interactionTarget = new InteractionTarget
                {
                    Position = unit.Config.FirstDestinationPosition,
                    InteractionDistance = unit.Config.UnitInteractionDistance
                };
                TransitionToState(State.Moving);
                return;
            }

            if (unit.Search.FindNearestTarget(out InteractionTarget interactionTarget))
            {
                // Target is an object nearby
                this.interactionTarget = interactionTarget;
                TransitionToState(State.Moving);
            }

            // Expand search radius if nothing was found.
            if (currentState == State.Searching) unit.ModifyCurrentSearchRadius(Time.deltaTime);
            // Reset search radius if something was found.
            else unit.ResetCurrentSearchRadius();
        }

        private void Move()
        {
            if (interactionTarget != null && unit.Movement.MoveTo(interactionTarget))
            {
                if (!unit.HasArrivedAtFirstDestination)
                {
                    unit.ArrivedAtFirstDestination();
                    TransitionToState(State.Searching);
                }
                else TransitionToState(State.Interacting);
            }
        }

        private void Interact()
        {
            StartCoroutine(PerformInteraction());
        }

        private IEnumerator PerformInteraction()
        {
            while (interactionTarget != null)
            {
                interactionTarget = unit.Interaction.PerformInteraction(interactionTarget);
                // Add visual effect, restore/reduce stamina
                yield return new WaitForSeconds(unit.Config.InteractionCooldown);
            }

            TransitionToState(State.Searching);
        }

        private void TransitionToState(State newState)
        {
            currentState = newState;
        }
    }
}