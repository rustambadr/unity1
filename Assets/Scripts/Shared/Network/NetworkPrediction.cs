using System.Collections.Generic;
using Mirage;
using UnityEngine;

namespace Platformer.Shared.Network
{
    [AddComponentMenu("Platformer/Network/NetworkPrediction")]
    public class NetworkPrediction : NetworkBehaviour
    {
        [Tooltip("Max snap count per one frame.")]
        public int maxSnapPerFrame = 500;

        [Tooltip("Max distance for trigger prediction error.")]
        public float predictionErrorDistance = 0.01f;

        private readonly List<SimulationStep> simulationSteps = new();

        public bool AllowPrediction => IsLocalPlayer;
        private Transform targetTransform => transform;

        private void FixedUpdate()
        {
            if (IsServerOnly) return;

            CollectSnap();
        }

        public bool HasPredictionError(SimulationStep step)
        {
            var temp = simulationSteps.Find(tempStep => tempStep.tickCount == step.tickCount);

            if (!temp.IsValid) return true;

            return Vector3.Distance(temp.position, step.position) > predictionErrorDistance;
        }

        /**
         * Создать и записать снимок состояния.
         */
        public void CollectSnap()
        {
            AppendSnap(CreateSnap());
        }

        /**
         * Создать снимок состояния.
         */
        private SimulationStep CreateSnap()
        {
            return new SimulationStep
            {
                tickCount = NetworkTickManager.tickCount,
                position = targetTransform.position,
                rotation = targetTransform.rotation,
                scale = targetTransform.localScale
            };
        }

        /**
         * Добавить снимок состояния в массив.
         */
        private void AppendSnap(SimulationStep snap)
        {
            if (simulationSteps.Count > maxSnapPerFrame) simulationSteps.RemoveAt(0);

            simulationSteps.Add(snap);
        }
    }
}