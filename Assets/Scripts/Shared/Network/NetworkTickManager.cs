// using UnityEngine;
// using Mirror; 
// using Platformer.Shared.Network.Message;
//
// namespace Platformer.Shared.Network
// {
//     public delegate void OnTick(int tick);
//
//     [AddComponentMenu("Platformer/Network/NetworkTickManager")]
//     public class NetworkTickManager: NetworkBehaviour
//     {
//         private const float TickRate = 1f / 60;
//
//         private float _tickTimer;
//         public static int tick;
//
//         /// <summary>
//         /// Main tick update event
//         /// </summary>
//         public event OnTick OnTick;
//
//         protected virtual void OnEnable()
//         {
//             NetworkLoop.OnEarlyUpdate += OnEarlyUpdate;
//
//             NetworkServer.RegisterHandler<NetworkTickMessage>(OnServerPing, false);
//         }
//
//         protected virtual void OnDisable()
//         {
//             if (NetworkClient.active)
//             {
//                 NetworkLoop.OnEarlyUpdate -= OnEarlyUpdate;
//             }
//
//             if (NetworkServer.active)
//             {
//                 NetworkServer.UnregisterHandler<NetworkTickMessage>();
//             }
//         }
//         
//         void OnServerPing(NetworkConnectionToClient conn, NetworkTickMessage message)
//         {
//             conn.identity.GetComponent<NetworkTransform>().lastClientTick = message.ServerTick;
//             // Debug.Log(message.ServerTick);
//         }
//         
//         public void OnEarlyUpdate()
//         {
//             if (!NetworkClient.active || !NetworkClient.localPlayer.GetComponent<NetworkPrediction>())
//             {
//                 return;
//             }
//             
//             NetworkTickMessage tickMessage = new NetworkTickMessage(tick);
//             NetworkClient.Send(tickMessage, Channels.Unreliable);
//
//             NetworkClient.localPlayer.GetComponent<NetworkPrediction>().CollectSnap();
//         }
//         
//         public void Update()
//         {
//             _tickTimer += Time.fixedDeltaTime;
//
//             if (_tickTimer >= TickRate)
//             {
//                 _tickTimer -= TickRate;
//                 tick++;
//                 OnTick?.Invoke(tick);
//             }
//         }
//
//         //
//         // public static int tickCount;
//         //
//         // private float tickTimer;
//         //
//         // public NetworkClient Client;
//         // public NetworkServer Server;
//         //
//         // public struct TickMessage
//         // {
//         //     public int tickCount;
//         // }
//         //
//         // private void Awake()
//         // {
//         //     tickTimer = 0f;
//         //     tickCount = 0;
//         //
//         //     if (Client)
//         //     {
//         //         Client.Connected.AddListener(OnClientConnect);
//         //     }
//         //
//         //     if (Server)
//         //     {
//         //         Server.Connected.AddListener(OnAuthenticated);
//         //     }
//         // }
//         //
//         // public void OnTickMessage(TickMessage message)
//         // {
//         //     tickCount = message.tickCount;
//         // }
//         //
//         // void OnClientConnect(INetworkPlayer player)
//         // {
//         //     Client.MessageHandler.RegisterHandler<TickMessage>(OnTickMessage);
//         // }
//         //
//         // void OnAuthenticated(INetworkPlayer player)
//         // {
//         //     TickMessage msg = new TickMessage()
//         //     {
//         //         tickCount = tickCount,
//         //     };
//         //
//         //     var list = new List<INetworkPlayer> { player };
//         //     NetworkServer.SendToMany(list.AsEnumerable(), msg);
//         // } 
//         //
//         // private void Update()
//         // {
//         //     if(!Client.IsConnected) return;
//         //     if (IsClientOnly) return;
//         //     
//         //     tickTimer += Time.deltaTime;
//         //     if (!(tickTimer >= Time.fixedDeltaTime)) return;
//         //     tickTimer = 0;
//         //     tickCount++;
//         //     
//         //     TickMessage msg = new TickMessage()
//         //     {
//         //         tickCount = tickCount,
//         //     };
//         //     
//         //     NetworkServer.SendToMany(Server.Players.AsEnumerable(), msg);
//         // }
//     }
// }