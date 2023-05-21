using Mirror;

namespace Platformer.Shared.Network.Message
{
    // [NetworkMessage]
    public struct NetworkTickMessage: NetworkMessage
    {
        public int ServerTick;
        public NetworkTickMessage(int value)
        {
            ServerTick = value;
        }
    }
}