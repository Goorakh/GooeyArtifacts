using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public class MovingInteractableRestState : MovingInteractableBaseState
    {
        static readonly SpawnCard _fallbackPositionHelperCard;

        static MovingInteractableRestState()
        {
            _fallbackPositionHelperCard = ScriptableObject.CreateInstance<SpawnCard>();
            _fallbackPositionHelperCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
            _fallbackPositionHelperCard.sendOverNetwork = false;
            _fallbackPositionHelperCard.name = "scPositionHelper_Fallback";
            _fallbackPositionHelperCard.hullSize = HullClassification.Human;
            _fallbackPositionHelperCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            _fallbackPositionHelperCard.requiredFlags = NodeFlags.None;
            _fallbackPositionHelperCard.forbiddenFlags = NodeFlags.NoChestSpawn;
        }

        const float MaxNodeSearchDistance = 100f;

        float _waitDuration;

        SpawnCard _nextPositionSelectorSpawnCard;
        bool _positionSelectorIsTemporary;

        float _searchDistance = 50f;

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority)
            {
                _waitDuration = UnityEngine.Random.Range(2.5f, 7.5f);

                if (spawnCard)
                {
                    _waitDuration *= Util.Remap(spawnCard.directorCreditCost, 0f, 50f, 0.75f, 2f);

                    _nextPositionSelectorSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                    _nextPositionSelectorSpawnCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
                    _nextPositionSelectorSpawnCard.sendOverNetwork = false;
                    _nextPositionSelectorSpawnCard.name = "scPositionHelper_" + spawnCard.name;
                    _nextPositionSelectorSpawnCard.hullSize = spawnCard.hullSize;
                    _nextPositionSelectorSpawnCard.nodeGraphType = spawnCard.nodeGraphType;
                    _nextPositionSelectorSpawnCard.requiredFlags = spawnCard.requiredFlags;
                    _nextPositionSelectorSpawnCard.forbiddenFlags = spawnCard.forbiddenFlags;

                    _positionSelectorIsTemporary = true;
                }
                else
                {
                    _nextPositionSelectorSpawnCard = _fallbackPositionHelperCard;

                    _positionSelectorIsTemporary = false;
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (_positionSelectorIsTemporary && _nextPositionSelectorSpawnCard)
            {
                Destroy(_nextPositionSelectorSpawnCard);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform)
                return;

            if (isAuthority && fixedAge >= _waitDuration)
            {
                Vector3? targetPosition = tryFindNextTargetPosition();
                if (targetPosition.HasValue)
                {
                    outer.SetNextState(new MovingInteractableMoveToTargetState
                    {
                        Destination = targetPosition.Value
                    });
                }
                else if (_searchDistance < MaxNodeSearchDistance)
                {
                    outer.SetNextState(new MovingInteractableRestState
                    {
                        _searchDistance = Mathf.Min(MaxNodeSearchDistance, _searchDistance + 25f)
                    });
                }
                else
                {
                    Log.Debug($"{gameObject}: no valid target position found, resetting state");

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
                maxDistance = _searchDistance * (1f + (2f * Mathf.Pow(UnityEngine.Random.value, 5f)))
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
