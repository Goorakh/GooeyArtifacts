using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public sealed class MovingInteractableSettleState : MovingInteractableBaseState
    {
        const float DURATION = 0.75f;

        static readonly float _jitterStrength = 0.15f;

        public Quaternion TargetRotation;
        public Vector3 TargetPosition;

        Quaternion _startRotation;
        Vector3 _startPosition;

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority)
            {
                if (spawnCard is InteractableSpawnCard interactableSpawnCard && interactableSpawnCard.slightlyRandomizeOrientation)
                {
                    TargetPosition += transform.TransformDirection(Vector3.down * 0.3f);
                }
            }

            if (transformSyncController)
            {
                transformSyncController.EnableClientTransformControl = true;
            }

            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);

            writer.Write(TargetRotation);
            writer.Write(TargetPosition);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);

            TargetRotation = reader.ReadQuaternion();
            TargetPosition = reader.ReadVector3();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            float fraction = Mathf.Clamp01(fixedAge / DURATION);

            Vector3 jitterOffset = UnityEngine.Random.insideUnitSphere * _jitterStrength;
            jitterOffset.y *= 0.2f;
            jitterOffset *= 1f - Mathf.Pow(fraction, 2f);
            
            if (transform)
            {
                transform.position = Vector3.Lerp(_startPosition, TargetPosition, fraction) + jitterOffset;
                transform.rotation = Quaternion.Slerp(_startRotation, TargetRotation, fraction);
            }

            if (fixedAge >= DURATION && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (transformSyncController)
            {
                transformSyncController.EnableClientTransformControl = false;
            }

            if (transform)
            {
                transform.position = TargetPosition;
                transform.rotation = TargetRotation;
            }
        }
    }
}
