using EntityStates;
using GooeyArtifacts.Artifacts.MovingInteractables;
using RoR2;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    public class MovingInteractableBaseState : EntityState
    {
        protected InteractableSpawnCard spawnCard { get; private set; }

        public new GameObject gameObject { get; private set; }

        public new Transform transform { get; private set; }

        public override void OnEnter()
        {
            base.OnEnter();

            InteractableMoveController moveController = GetComponent<InteractableMoveController>();

            spawnCard = moveController.SpawnCardServer;
            gameObject = moveController.InteractableObject;

            if (gameObject)
            {
                transform = gameObject.transform;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform || !gameObject)
            {
                outer.SetNextStateToMain();
            }
        }
    }
}
