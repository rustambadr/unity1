using System.Collections.Generic;
using Mirage;
using Platformer.Shared.Physics;
using UnityEngine;

namespace Platformer.Shared.Player
{
    [AddComponentMenu("Platformer/Player/Controller")]
    public class Controller : NetworkBehaviour
    {
        // [SerializeField]
        // public MoveType moveType = MoveType.None;

        List<MoveCmd> moveCmds = new();

        KinematicObject kinematicObject;

        protected virtual void OnEnable()
        {
            kinematicObject = GetComponent<KinematicObject>();
        }

        public virtual void Update()
        {
            if (IsLocalPlayer)
            {
                kinematicObject.ApplyCmd(CreateMove());
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsServer)
            {
                SimulateMove();
            }

            //Debug.Log(Time.deltaTime);
        }


        private void SimulateMove()
        {
            //Debug.Log("Count package" + moveCmds.Count);

            try
            {
                foreach (MoveCmd moveCmd in moveCmds)
                {
                    kinematicObject.ApplyCmd(moveCmd);
                }
            }
            finally
            {
                moveCmds.Clear();
            }
        }

        [LocalPlayer]
        private MoveCmd CreateMove()
        {
            MoveCmd moveCmd = new MoveCmd();

            moveCmd.horizontalInput = Input.GetAxis("Horizontal");
            moveCmd.verticalInput = Input.GetAxis("Vertical");
            moveCmd.buttons = 0;

            if (moveCmd.horizontalInput > 0)
            {
                moveCmd.buttons |= Buttons.IN_FORWARD;
            }

            if (moveCmd.horizontalInput < 0)
            {
                moveCmd.buttons |= Buttons.IN_BACK;
            }

            if (Input.GetButton("Jump"))
            {
                moveCmd.buttons |= Buttons.IN_JUMP;
            }

            CmdMove(moveCmd);

            return moveCmd;
        }

        [ServerRpc]
        void CmdMove(MoveCmd moveCmd)
        {
            moveCmds.Add(moveCmd);
        }

        /*protected ContactFilter2D contactFilter;
        protected RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

        protected virtual void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
        }

        protected virtual void Start()
        {
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            contactFilter.useLayerMask = true;
        }

        protected virtual void FixedUpdate()
        {
            switch(moveType)
            {
                case MoveType.None:
                    break;
                case MoveType.Normal:
                    MoveNormal();
                    break;
                default:
                    throw new System.Exception("Unknown movetype.");
            }
            velocity += Physics2D.gravity * Time.deltaTime;

            float distance = velocity.magnitude; 
            var count = body.Cast(Vector2.down, contactFilter, hitBuffer, distance);
            if (count != 0)
            {
                velocity.y = 0;
                
*/ /*                Rigidbody2D otherBody = hit.collider.GetComponent<Rigidbody2D>();
                if (otherBody != null)
                {
                    // ��������� ������������
                }*/ /*
            }

            body.position += velocity.normalized * distance;
        }

        private void MoveNormal()
        {

        }*/
    }
}