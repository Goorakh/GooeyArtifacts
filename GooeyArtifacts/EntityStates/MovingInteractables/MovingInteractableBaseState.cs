using EntityStates;
using GooeyArtifacts.Artifacts.MovingInteractables;
using GooeyArtifacts.Utils;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    public class MovingInteractableBaseState : EntityState
    {
        protected SpawnCard spawnCard { get; private set; }

        public new GameObject gameObject { get; private set; }

        public new Transform transform { get; private set; }

        protected SyncExternalNetworkedObjectTransform transformSyncController { get; private set; }

        protected MapNodeGroup.GraphType nodeGraphType { get; private set; }
        protected HullClassification hullSize { get; private set; }
        protected bool occupyPosition { get; private set; }

        public override void OnEnter()
        {
            base.OnEnter();

            InteractableMoveController moveController = GetComponent<InteractableMoveController>();

            spawnCard = moveController.SpawnCardServer;
            gameObject = moveController.InteractableObject;
            transformSyncController = moveController.TransformSyncController;

            if (gameObject)
            {
                transform = gameObject.transform;
            }

            if (isAuthority)
            {
                if (spawnCard)
                {
                    nodeGraphType = spawnCard.nodeGraphType;
                    hullSize = spawnCard.hullSize;
                    occupyPosition = spawnCard.occupyPosition;
                }
                else
                {
                    nodeGraphType = MapNodeGroup.GraphType.Ground;
                    hullSize = HullClassification.Human;
                    occupyPosition = true;
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if ((!transform || !gameObject) && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }
    }
}
