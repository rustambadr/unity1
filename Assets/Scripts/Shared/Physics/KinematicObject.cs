using Cysharp.Threading.Tasks;
using Platformer.Shared.Player;
using UnityEngine;

namespace Platformer.Shared.Physics
{
    [AddComponentMenu("Platformer/Physics/KinematicObject")]
    [DefaultExecutionOrder(1)]
    public class KinematicObject : MonoBehaviour
    {
        /*        [SerializeField]
                public MoveType moveType = MoveType.None;*/

        protected const float shellRadius = 0.01f;
        
        public float maxSpeed = 5;
        public float jumpSpeed = 1;
        Vector2 moveDir;

        Rigidbody2D bodyRigidbody2D;

        bool isGround;
        bool isJump;

        protected ContactFilter2D contactFilter;
        protected RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

        protected virtual void OnEnable()
        {
            bodyRigidbody2D = GetComponent<Rigidbody2D>();
        }

        protected virtual void Start()
        {
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            contactFilter.useLayerMask = true;
        }

        public void ApplyCmd(MoveCmd moveCmd)
        {
            float horizontal = Mathf.Clamp(moveCmd.horizontalInput, -1f, 1f);
            if (horizontal != 0)
            {
                moveDir.x = horizontal * maxSpeed;
            }

            if (!isJump && (moveCmd.buttons & Buttons.IN_JUMP) != 0)
            {
                moveDir.y = jumpSpeed;
                isJump = true;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (moveDir != Vector2.zero)
            {
                bodyRigidbody2D.velocity = new Vector2(0, bodyRigidbody2D.velocity.y + moveDir.y);
            }

            Vector2 deltaPosition = new Vector2(moveDir.x, 0) * Time.deltaTime;
            float distance = deltaPosition.magnitude;

            //check if we hit anything in current direction of travel
            var count = bodyRigidbody2D.Cast(deltaPosition, contactFilter, hitBuffer, distance + shellRadius);
            for (var i = 0; i < count; i++)
            {
                //remove shellDistance from actual move distance.
                float modifiedDistance = hitBuffer[i].distance - shellRadius;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }

            if (count > 0)
            {
                //Debug.Log("Distance " + distance + " deltaPosition " + deltaPosition);
            }

            moveDir = new Vector2(0, 0);

            bodyRigidbody2D.position = bodyRigidbody2D.position + deltaPosition.normalized * distance;

            //JumpUpdate();

            //velocity += Physics2D.gravity * Time.deltaTime;

            //float distance = velocity.magnitude;
            //var count = body.Cast(Vector2.down, contactFilter, hitBuffer, distance);
            //if (count != 0)
            //{
            //    velocity.y = 0;

            //    *//*                Rigidbody2D otherBody = hit.collider.GetComponent<Rigidbody2D>();
            //    if (otherBody != null)
            //    {
            //        // Обработка столкновения
            //    }
            //    *//*
            //}

            //body.position += velocity.normalized * distance;


            GravityUpdate();

            //lastMovementImpulse = movementImpulse;
        }

        // Применяем гравитацию.
        private void GravityUpdate()
        {
            isGround = bodyRigidbody2D.Cast(Vector2.down, contactFilter, hitBuffer, 0.1f) != 0;

            if (isGround)
            {
                isJump = false;
            }

            //if (!isGround)
            //{
            //    bodyRigidbody2D.velocity -= Physics2D.gravity * Time.deltaTime;
            //}
        }

        //private void JumpUpdate()
        //{
        //    if (isGround && lastMovementImpulse.y == 0f && movementImpulse.y != 0f)
        //    {
        //        bodyRigidbody2D.velocity += new Vector2(bodyRigidbody2D.velocity.x, bodyRigidbody2D.velocity.y + 5f);
        //        isJump = true;
        //        isGround = false;
        //    }

        //    if (isGround)
        //    {
        //        isJump = false;
        //    }
        //}

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
                
*//*                Rigidbody2D otherBody = hit.collider.GetComponent<Rigidbody2D>();
                if (otherBody != null)
                {
                    // Обработка столкновения
                }*//*
            }

            body.position += velocity.normalized * distance;
        }

        private void MoveNormal()
        {

        }*/
    }
}