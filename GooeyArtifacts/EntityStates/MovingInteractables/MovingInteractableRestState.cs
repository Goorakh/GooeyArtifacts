using GooeyArtifacts.Utils;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public class MovingInteractableRestState : MovingInteractableBaseState
    {
        float _waitDuration;

        SpawnCard _nextPositionSelectorSpawnCard;

        public override void OnEnter()
        {
            base.OnEnter();

            _waitDuration = UnityEngine.Random.Range(2.5f, 7.5f) * Util.Remap(spawnCard.directorCreditCost, 0f, 50f, 0.75f, 2f);

            _nextPositionSelectorSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();
            _nextPositionSelectorSpawnCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
            _nextPositionSelectorSpawnCard.name = "scPositionHelper_" + spawnCard.name;
            _nextPositionSelectorSpawnCard.sendOverNetwork = false;
            _nextPositionSelectorSpawnCard.hullSize = spawnCard.hullSize;
            _nextPositionSelectorSpawnCard.nodeGraphType = spawnCard.nodeGraphType;
            _nextPositionSelectorSpawnCard.requiredFlags = spawnCard.requiredFlags;
            _nextPositionSelectorSpawnCard.forbiddenFlags = spawnCard.forbiddenFlags;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (_nextPositionSelectorSpawnCard)
            {
                Destroy(_nextPositionSelectorSpawnCard);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform)
                return;

            if (fixedAge >= _waitDuration)
            {
                Vector3? targetPosition = tryFindNextTargetPosition();
                if (targetPosition.HasValue)
                {
                    outer.SetNextState(new MovingInteractableMoveToTargetState
                    {
                        Destination = targetPosition.Value
                    });
                }
                else
                {
                    outer.SetNextStateToMain();
                }
            }
        }

        Vector3? tryFindNextTargetPosition()
        {
            DirectorCore directorCore = DirectorCore.instance;
            if (!directorCore)
                return null;

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = transform.position,
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = 20f,
                maxDistance = 50f + (Mathf.Pow(RoR2Application.rng.nextNormalizedFloat, 2.25f) * 150f)
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_nextPositionSelectorSpawnCard, placementRule, RoR2Application.rng);

            GameObject positionMarker = directorCore.TrySpawnObject(spawnRequest);
            if (!positionMarker)
                return null;

            Vector3 position = positionMarker.transform.position;
            Destroy(positionMarker);
            return position;
        }
    }
}
