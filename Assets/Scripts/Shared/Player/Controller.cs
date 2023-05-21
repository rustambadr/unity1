using System;
using System.Collections.Generic;
using Mirror;
using Platformer.Shared.Network.Prediction;
using Platformer.Shared.Physics;
using UnityEngine;

namespace Platformer.Shared.Player
{
    [AddComponentMenu("Platformer/Player/Controller")]
    public class Controller : NetworkBehaviour
    {
        /// <summary>
        /// Буфер для хранения последних введенных команд.
        /// </summary>
        private readonly List<MoveCmd> _moveCmdList = new();

        /// <summary>
        /// Компонент кинематического движения.
        /// </summary>
        private KinematicObject _kinematicObject;

        /// <summary>
        /// Компонент предсказания движения.
        /// </summary>
        private NetworkPrediction _networkPrediction;

        // private void Update()
        // {
        //     if (isLocalPlayer)
        //     {
        //         HandleClientCommand();
        //     }
        // }

        private void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                HandleClientCommand();
            }
            
            if (isServer)
            {
                HandleServerCommand();
            }
        }

        private void OnEnable()
        {
            _kinematicObject = GetComponent<KinematicObject>();
            if (_kinematicObject == null)
            {
                throw new Exception("Не найден компонент KinematicObject.");
            }
            
            _networkPrediction = GetComponent<NetworkPrediction>();
            if (_networkPrediction == null)
            {
                throw new Exception("Не найден компонент NetworkPrediction.");
            }
        }

        /// <summary>
        /// Сбор введенных команд и отправка на сервер.
        /// </summary>
        private void HandleClientCommand()
        {
            MoveCmd cmd = CreateMoveCommand();

            // if (!cmd.IsValid())
            // {
                // return;
            // }

            _kinematicObject.ApplyCmd(cmd);
            _networkPrediction.CreateSnapshot(cmd.LocalTime);

            ServerReceiveMoveCommand(cmd);
        }

        /// <summary>
        /// Обработка введенных команд и отправка подтверждений на клиент.
        /// </summary>
        private void HandleServerCommand()
        {
            try
            {
                foreach (MoveCmd cmd in _moveCmdList)
                {
                    _kinematicObject.ApplyCmd(cmd);
                    _networkPrediction.ServerSendSnapshot(cmd.LocalTime);
                }
            }
            finally
            {
                _moveCmdList.Clear();
            }
        }

        /// <summary>
        /// Создание объекта команды с введенными данными клиента.
        /// </summary>
        private MoveCmd CreateMoveCommand()
        {
            MoveCmd moveCmd = new MoveCmd
            {
                HorizontalInput = Input.GetAxis("Horizontal"),
                VerticalInput = Input.GetAxis("Vertical"),
                Buttons = 0,
                LocalTime = NetworkTime.localTime,
            };

            if (moveCmd.HorizontalInput > 0)
            {
                moveCmd.Buttons |= Buttons.IN_FORWARD;
            }

            if (moveCmd.HorizontalInput < 0)
            {
                moveCmd.Buttons |= Buttons.IN_BACK;
            }

            if (Input.GetButton("Jump"))
            {
                moveCmd.Buttons |= Buttons.IN_JUMP;
            }

            return moveCmd;
        }

        /// <summary>
        /// Принимаем команду от клиента.
        /// </summary>
        [Command]
        private void ServerReceiveMoveCommand(MoveCmd moveCmd)
        {
            // todo Добавить ограничение по кол-ву команд.
            _moveCmdList.Add(moveCmd);
        }
    }
}