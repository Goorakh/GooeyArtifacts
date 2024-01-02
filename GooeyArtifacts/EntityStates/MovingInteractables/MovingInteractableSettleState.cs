using UnityEngine;

namespace GooeyArtifacts.EntityStates.MovingInteractables
{
    [EntityStateType]
    public class MovingInteractableSettleState : MovingInteractableBaseState
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

            if (spawnCard && spawnCard.slightlyRandomizeOrientation)
            {
                TargetPosition += transform.TransformDirection(Vector3.down * 0.3f);
            }

            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!transform)
                return;

            if (fixedAge < DURATION)
            {
                float fraction = fixedAge / DURATION;

                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * _jitterStrength;
                randomOffset.y *= 0.2f;

                transform.position = Vector3.Lerp(_startPosition, TargetPosition, fraction) + Vector3.Lerp(randomOffset, Vector3.zero, fraction);
                transform.rotation = Quaternion.Slerp(_startRotation, TargetRotation, fraction);
            }
            else
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (transform)
            {
                transform.position = TargetPosition;
                transform.rotation = TargetRotation;
            }
        }
    }
}
