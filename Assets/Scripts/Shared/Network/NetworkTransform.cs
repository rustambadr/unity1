using Mirror;
using Platformer.Shared.Network.Prediction;
using UnityEngine;

namespace Platformer.Shared.Network
{
    public struct SimulationStep
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public double Time;

        public bool IsValid() => Time != 0;
    }

    [AddComponentMenu("Platformer/Network/NetworkTransform")]
    public class NetworkTransform : NetworkBehaviour
    {
        // [Tooltip("Set to true if updates from server should be ignored by owner")]
        // public bool excludeOwnerUpdate = true;

        [Header("Synchronization")] [Tooltip("Set to true if position should be synchronized")]
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

        [Header("Diagnostics")] public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastScale;

        private SimulationStep _lastServerSnap;
        private NetworkPrediction _networkPrediction;

        private Transform TargetTransform => transform;

        private bool HasMoved => syncPosition && Vector3.SqrMagnitude(lastPosition - TargetTransform.position) >
            localPositionSensitivity * localPositionSensitivity;

        private bool HasRotated => syncRotation && Quaternion.Angle(lastRotation, TargetTransform.rotation) >
            localRotationSensitivity;

        private bool HasScaled => syncScale && Vector3.SqrMagnitude(lastScale - TargetTransform.localScale) >
            localScaleSensitivity * localScaleSensitivity;

        private void FixedUpdate()
        {
            if (isServer)
            {
                if (HasEitherMovedRotatedScaled())
                    RpcMove(TargetTransform.position, TargetTransform.rotation, TargetTransform.localScale);
            }

            if (isClient && _lastServerSnap.IsValid())
            {
                ApplyPositionRotationScale(_lastServerSnap);
            }
        }

        private void OnEnable()
        {
            _networkPrediction = GetComponent<NetworkPrediction>();
        }

        [ClientRpc]
        private void RpcMove(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SimulationStep simulationStep = new SimulationStep
            {
                Position = position,
                Rotation = rotation,
                Scale = scale,
                Time = NetworkTime.time,
            };

            if (!isServer) SetGoal(simulationStep);
        }

        private void SetGoal(SimulationStep step)
        {
            _lastServerSnap = step;
        }

        private bool HasEitherMovedRotatedScaled()
        {
            var changed = HasMoved || HasRotated || HasScaled;
            if (changed)
            {
                if (syncPosition) lastPosition = TargetTransform.position;

                if (syncRotation) lastRotation = TargetTransform.rotation;

                if (syncScale) lastScale = TargetTransform.localScale;
            }

            return changed;
        }

        private void ApplyPositionRotationScale(SimulationStep step)
        {
            if (isLocalPlayer)
            {
                if (!_networkPrediction.HasError)
                {
                    return;
                }

                _networkPrediction.ResetError();
            }

            if (syncPosition) TargetTransform.position = step.Position;

            if (syncRotation) TargetTransform.rotation = step.Rotation;

            if (syncScale) TargetTransform.localScale = step.Scale;
        }
    }
}