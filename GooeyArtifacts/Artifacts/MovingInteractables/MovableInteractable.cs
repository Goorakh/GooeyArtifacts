using RoR2;
using System;
using UnityEngine;

namespace GooeyArtifacts.Artifacts.MovingInteractables
{
    public class MovableInteractable : MonoBehaviour
    {
        public static event Action<MovableInteractable> OnMovableInteractableCreated;

        public InteractableSpawnCard SpawnCard;

        public bool IsClaimed;

        void Start()
        {
            OnMovableInteractableCreated?.Invoke(this);
            InstanceTracker.Add(this);
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);
        }
    }
}
