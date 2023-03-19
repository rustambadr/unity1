using System.Collections.Generic;
using System.Linq;
using Mirage;
using UnityEngine;

namespace Platformer.Shared.Network
{
    [AddComponentMenu("Platformer/Network/Network Tick Manager")]
    public class NetworkTickManager : MonoBehaviour
    {
        public static int tickCount;

        private float tickTimer;

        public NetworkClient Client;
        public NetworkServer Server;

        public struct TickMessage
        {
            public int tickCount;
        }

        private void Awake()
        {
            tickTimer = 0f;
            tickCount = 0;

            if (Client)
            {
                Client.Connected.AddListener(OnClientConnect);
            }

            if (Server)
            {
                Server.Connected.AddListener(OnAuthenticated);
            }
        }

        public void OnTickMessage(TickMessage message)
        {
            tickCount = message.tickCount;
        }

        void OnClientConnect(INetworkPlayer player)
        {
            Client.MessageHandler.RegisterHandler<TickMessage>(OnTickMessage);
        }

        void OnAuthenticated(INetworkPlayer player)
        {
            TickMessage msg = new TickMessage()
            {
                tickCount = tickCount,
            };

            var list = new List<INetworkPlayer> { player };
            NetworkServer.SendToMany(list.AsEnumerable(), msg);
        } 

        private void Update()
        {
            tickTimer += Time.deltaTime;
            if (!(tickTimer >= Time.fixedDeltaTime)) return;
            tickTimer = 0;
            tickCount++;
        }
    }
}