using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Platformer.Shared.Network.Prediction
{
    [AddComponentMenu("Platformer/Network/Prediction/NetworkPrediction")]
    public class NetworkPrediction : NetworkBehaviour
    {
        public float predictionErrorDistance = 0.1f;

        /// <summary>
        /// Буфер для хранения последних просчитанных позиций на клиенте.
        /// </summary>
        private List<PredictionSnapshot> _snapshotList = new();

        /// <summary>
        /// Имеется ли ошибка предсказания.
        /// </summary>
        [NonSerialized]
        public bool HasError = true;

        private Transform TargetTransform => transform;

        /// <summary>
        /// Создание снимка состояния объкта.
        /// </summary>
        public void CreateSnapshot(double time)
        {
            PredictionSnapshot snapshot = new PredictionSnapshot
            {
                LocalTime = time,
                Position = TargetTransform.position,
            };

            _snapshotList.Add(snapshot);
        }

        /// <summary>
        /// Проверка снимка состояния объкта.
        /// </summary>
        public void ServerSendSnapshot(double time)
        {
            PredictionSnapshot snapshot = new PredictionSnapshot
            {
                LocalTime = time,
                Position = TargetTransform.position,
            };

            ClientReceiveCheckSnapshot(snapshot);
        }

        public void ResetError()
        {
            HasError = false;
        }

        [ClientRpc]
        private void ClientReceiveCheckSnapshot(PredictionSnapshot predictionSnapshot)
        {
            if (_snapshotList.Count == 0)
            {
                HasError = true;
                
                return;
            }
            
            PredictionSnapshot snapshot = _snapshotList.First();
            if (
                snapshot.LocalTime != predictionSnapshot.LocalTime ||
                Vector3.Distance(snapshot.Position, predictionSnapshot.Position) >= predictionErrorDistance
            )
            {
                HasError = true;

                _snapshotList.Clear();
            }
            else
            {
                _snapshotList.Remove(snapshot);
            }
        }
    }
}