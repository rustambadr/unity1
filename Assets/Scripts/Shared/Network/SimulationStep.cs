using UnityEngine;

namespace Platformer.Shared.Network
{
    public struct SimulationStep
    {
        public int tickCount;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public bool IsValid => tickCount > 0;
    }
}