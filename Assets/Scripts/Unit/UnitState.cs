using System;
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
        private bool interacting = false;
        private InteractionTarget.InteractionType lastInteractionType;

        private void Awake()
        {
            unit = GetComponent<Unit>();

            unit.DelegateManager.OnUnitSetUp += DelegateManager_OnUnitSetUp;
            unit.DelegateManager.OnInteractionTargetDespawn += DelegateManager_OnInteractionTargetDespawn;
        }

        private void DelegateManager_OnInteractionTargetDespawn()
        {
            currentState = State.Searching;

            if (interactionTarget == null)
            {
                // Clean subscription and re-subscribe
                // So it wouldn't be triggered when despawned interaction target is respawned and despawned again.
                unit.DelegateManager.OnInteractionTargetDespawn = null;
                unit.DelegateManager.OnInteractionTargetDespawn += DelegateManager_OnInteractionTargetDespawn;
                return;
            }
            if (interactionTarget.Unit != null) interactionTarget.Unit.DelegateManager.OnDespawn -= unit.DelegateManager.OnInteractionTargetDespawn;
            if (interactionTarget.Resource != null) interactionTarget.Resource.OnDespawn -= unit.DelegateManager.OnInteractionTargetDespawn;
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
            if (!interacting) StartCoroutine(PerformInteraction());
        }

        private IEnumerator PerformInteraction()
        {
            while (interactionTarget != null)
            {
                interacting = true;
                lastInteractionType = interactionTarget.Interaction;
                interactionTarget = unit.Interaction.PerformInteraction(interactionTarget);
                if (interactionTarget == null && lastInteractionType == InteractionTarget.InteractionType.Unload) TransitionToState(State.Idle);
                // Add visual effect, restore/reduce stamina
                yield return new WaitForSeconds(unit.Config.InteractionCooldown);
            }

            interacting = false;
            TransitionToState(State.Searching);
        }

        private void TransitionToState(State newState)
        {
            currentState = newState;

            if (currentState == State.Idle)
            {
                lastInteractionType = InteractionTarget.InteractionType.None;
                unit.DelegateManager.OnUnitSetUp += DelegateManager_OnUnitSetUp;
            }
        }
    }
}