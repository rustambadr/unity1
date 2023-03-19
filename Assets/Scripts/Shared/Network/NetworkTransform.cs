using Mirage;
using UnityEngine;

namespace Platformer.Shared.Network
{
    [AddComponentMenu("Platformer/Network/NetworkTransform")]
    public class NetworkTransform : NetworkBehaviour
    {
        // [Tooltip("Set to true if updates from server should be ignored by owner")]
        // public bool excludeOwnerUpdate = true;

        [Header("Synchronization")] 
        [Tooltip("Set to true if position should be synchronized")]
        public bool syncPosition = true;

        [Tooltip("Set to true if rotation should be synchronized")]
        public bool syncRotation = true;

        [Tooltip("Set to true if scale should be synchronized")]
        public bool syncScale = true;

        [Header("Sensitivity")]
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localPositionSensitivity = .01f;

        [Tooltip("If rotation exceeds this angle, it will be transmitted on the network")]
        public float localRotationSensitivity = .01f;

        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localScaleSensitivity = .01f;

        [Header("Diagnostics")]
        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastScale;

        private SimulationStep lastServerSnap;

        private NetworkPrediction networkPrediction;

        private Transform targetTransform => transform;

        private bool HasMoved => syncPosition && Vector3.SqrMagnitude(lastPosition - targetTransform.position) >
            localPositionSensitivity * localPositionSensitivity;

        private bool HasRotated => syncRotation && Quaternion.Angle(lastRotation, targetTransform.rotation) >
            localRotationSensitivity;

        private bool HasScaled => syncScale && Vector3.SqrMagnitude(lastScale - targetTransform.localScale) >
            localScaleSensitivity * localScaleSensitivity;

        public bool IsPredicted => networkPrediction != null;

        private void FixedUpdate()
        {
            if (IsServer)
            {
                if (IsPredicted) networkPrediction.CollectSnap();

                if (HasEitherMovedRotatedScaled())
                    RpcMove(targetTransform.position, targetTransform.rotation, targetTransform.localScale);
            }

            if (IsClient)
            {
                if (lastServerSnap.IsValid)
                    ApplyPositionRotationScale(lastServerSnap);
            }
        }

        private void OnEnable()
        {
            networkPrediction = GetComponent<NetworkPrediction>();
        }

        [ClientRpc]
        private void RpcMove(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var temp = CreateSnap(position, rotation, scale);

            if (!IsServer) SetGoal(temp);
        }

        private void SetGoal(SimulationStep step)
        {
            lastServerSnap = step;
        }

        private bool HasEitherMovedRotatedScaled()
        {
            var changed = HasMoved || HasRotated || HasScaled;
            if (changed)
            {
                if (syncPosition) lastPosition = targetTransform.position;

                if (syncRotation) lastRotation = targetTransform.rotation;

                if (syncScale) lastScale = targetTransform.localScale;
            }

            return changed;
        }

        private void ApplyPositionRotationScale(SimulationStep step)
        {
            if (IsPredicted && IsLocalPlayer)
            {
                var error = networkPrediction.HasPredictionError(step);
                if (!error)
                {
                    return;
                }
                
                if(error)
                {
                    if (Application.isEditor)
                    {
                        Debug.Log("Prediction error.");
                    }
                }
            }
            
            if (syncPosition) targetTransform.position = step.position;

            if (syncRotation) targetTransform.rotation = step.rotation;

            if (syncScale) targetTransform.localScale = step.scale;
        }

        /**
         * Создать снимок состояния.
         */
        private SimulationStep CreateSnap(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return new SimulationStep
            {
                tickCount = NetworkTickManager.tickCount,
                position = position,
                rotation = rotation,
                scale = scale
            };
        }
    }
}