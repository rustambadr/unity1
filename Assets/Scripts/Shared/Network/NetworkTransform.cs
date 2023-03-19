using Mirage;
using UnityEngine;

namespace Platformer.Shared.Player
{
    [AddComponentMenu("Platformer/Network/NetworkTransform")]
    public class NetworkTransform : NetworkBehaviour
    {
        // target transform to sync. can be on a child.
        protected Transform TargetTransform => transform;

        [Header("Authority")]
        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        public bool clientAuthority;

        [Tooltip("Set to true if updates from server should be ignored by owner")]
        public bool excludeOwnerUpdate = true;

        [Header("Synchronization")] [Tooltip("Set to true if position should be synchronized")]
        public bool syncPosition = true;

        [Tooltip("Set to true if rotation should be synchronized")]
        public bool syncRotation = true;

        [Tooltip("Set to true if scale should be synchronized")]
        public bool syncScale = true;

        [Header("Interpolation")] [Tooltip("Set to true if position should be interpolated")]
        public bool interpolatePosition = true;

        [Tooltip("Set to true if rotation should be interpolated")]
        public bool interpolateRotation = true;

        [Tooltip("Set to true if scale should be interpolated")]
        public bool interpolateScale = true;

        // Sensitivity is added for VR where human players tend to have micro movements so this can quiet down
        // the network traffic.  Additionally, rigidbody drift should send less traffic, e.g very slow sliding / rolling.
        [Header("Sensitivity")]
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localPositionSensitivity = .01f;

        [Tooltip("If rotation exceeds this angle, it will be transmitted on the network")]
        public float localRotationSensitivity = .01f;

        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localScaleSensitivity = .01f;

        [Header("Diagnostics")]

        // server
        public Vector3 lastPosition;

        public Quaternion lastRotation;
        public Vector3 lastScale;

        // client
        // use local position/rotation for VR support
        [System.Serializable]
        public struct DataPoint
        {
            public float timeStamp;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public float movementSpeed;

            public bool IsValid => timeStamp != 0;
        }

        // Is this a client with authority over this transform?
        // This component could be on the player object or any object that has been assigned authority to this client.
        private bool IsOwnerWithClientAuthority => HasAuthority && clientAuthority;

        // interpolation start and goal
        public DataPoint start = new DataPoint();
        public DataPoint goal = new DataPoint();

        private void FixedUpdate()
        {
            // if server then always sync to others.
            // let the clients know that this has moved
            if (IsServer && HasEitherMovedRotatedScaled())
            {
                RpcMove(TargetTransform.localPosition, TargetTransform.localRotation, TargetTransform.localScale);
            }

            if (IsClient)
            {
                // send to server if we have local authority (and aren't the server)
                // -> only if connectionToServer has been initialized yet too
                if (IsOwnerWithClientAuthority)
                {
                    if (!IsServer && HasEitherMovedRotatedScaled())
                    {
                        // serialize
                        // local position/rotation for VR support
                        // send to server
                        CmdClientToServerSync(TargetTransform.localPosition, TargetTransform.localRotation,
                            TargetTransform.localScale);
                    }
                }
                else if (goal.IsValid)
                {
                    // teleport or interpolate
                    if (NeedsTeleport())
                    {
                        // local position/rotation for VR support
                        ApplyPositionRotationScale(goal.localPosition, goal.localRotation, goal.localScale);

                        // reset data points so we don't keep interpolating
                        start = new DataPoint();
                        goal = new DataPoint();
                    }
                    else
                    {
                        // local position/rotation for VR support
                        ApplyPositionRotationScale(InterpolatePosition(start, goal, TargetTransform.localPosition),
                            InterpolateRotation(start, goal, TargetTransform.localRotation),
                            InterpolateScale(start, goal, TargetTransform.localScale));
                    }
                }
            }
        }

        // moved or rotated or scaled since last time we checked it?
        private bool HasEitherMovedRotatedScaled()
        {
            // Save last for next frame to compare only if change was detected, otherwise
            // slow moving objects might never sync because of C#'s float comparison tolerance.
            // See also: https://github.com/vis2k/Mirror/pull/428)
            var changed = HasMoved || HasRotated || HasScaled;
            if (changed)
            {
                // local position/rotation for VR support
                if (syncPosition) lastPosition = TargetTransform.localPosition;
                if (syncRotation) lastRotation = TargetTransform.localRotation;
                if (syncScale) lastScale = TargetTransform.localScale;
            }

            return changed;
        }

        // local position/rotation for VR support
        // SqrMagnitude is faster than Distance per Unity docs
        // https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html

        private bool HasMoved => syncPosition && Vector3.SqrMagnitude(lastPosition - TargetTransform.localPosition) >
            localPositionSensitivity * localPositionSensitivity;

        private bool HasRotated => syncRotation &&
                                   Quaternion.Angle(lastRotation, TargetTransform.localRotation) >
                                   localRotationSensitivity;

        private bool HasScaled => syncScale && Vector3.SqrMagnitude(lastScale - TargetTransform.localScale) >
            localScaleSensitivity * localScaleSensitivity;

        // teleport / lag / stuck detection
        // - checking distance is not enough since there could be just a tiny fence between us and the goal
        // - checking time always works, this way we just teleport if we still didn't reach the goal after too much time has elapsed
        private bool NeedsTeleport()
        {
            // calculate time between the two data points
            var startTime = start.IsValid ? start.timeStamp : Time.time - Time.fixedDeltaTime;
            var goalTime = goal.IsValid ? goal.timeStamp : Time.time;
            var difference = goalTime - startTime;
            var timeSinceGoalReceived = Time.time - goalTime;
            return timeSinceGoalReceived > difference * 5;
        }

        // local authority client sends sync message to server for broadcasting
        [ServerRpc]
        private void CmdClientToServerSync(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            // deserialize payload
            SetGoal(position, rotation, scale);

            // server-only mode does no interpolation to save computations, but let's set the position directly
            if (IsServer && !IsClient)
                ApplyPositionRotationScale(goal.localPosition, goal.localRotation, goal.localScale);

            RpcMove(position, rotation, scale);
        }

        [ClientRpc]
        private void RpcMove(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (HasAuthority || excludeOwnerUpdate) return;

            if (!IsServer)
                SetGoal(position, rotation, scale);
        }

        // serialization is needed by OnSerialize and by manual sending from authority
        private void SetGoal(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // put it into a data point immediately
            var temp = new DataPoint
            {
                // deserialize position
                localPosition = position,
                localRotation = rotation,
                localScale = scale,
                timeStamp = Time.time
            };

            // movement speed: based on how far it moved since last time has to be calculated before 'start' is overwritten
            temp.movementSpeed = EstimateMovementSpeed(goal, temp, TargetTransform, Time.fixedDeltaTime);

            // reassign start wisely
            // first ever data point? then make something up for previous one so that we can start interpolation without waiting for next.
            if (start.timeStamp == 0)
            {
                start = new DataPoint
                {
                    timeStamp = Time.time - Time.fixedDeltaTime,
                    // local position/rotation for VR support
                    localPosition = TargetTransform.localPosition,
                    localRotation = TargetTransform.localRotation,
                    localScale = TargetTransform.localScale,
                    movementSpeed = temp.movementSpeed
                };
            }
            // second or nth data point? then update previous
            // but: we start at where ever we are right now, so that it's perfectly smooth and we don't jump anywhere
            //
            //    example if we are at 'x':
            //
            //        A--x->B
            //
            //    and then receive a new point C:
            //
            //        A--x--B
            //              |
            //              |
            //              C
            //
            //    then we don't want to just jump to B and start interpolation:
            //
            //              x
            //              |
            //              |
            //              C
            //
            //    we stay at 'x' and interpolate from there to C:
            //
            //           x..B
            //            \ .
            //             \.
            //              C
            //
            else
            {
                var oldDistance = Vector3.Distance(start.localPosition, goal.localPosition);
                var newDistance = Vector3.Distance(goal.localPosition, temp.localPosition);

                start = goal;

                // local position/rotation for VR support
                // teleport / lag / obstacle detection: only continue at current position if we aren't too far away
                // XC  < AB + BC (see comments above)
                if (Vector3.Distance(TargetTransform.localPosition, start.localPosition) < oldDistance + newDistance)
                {
                    start.localPosition = TargetTransform.localPosition;
                    start.localRotation = TargetTransform.localRotation;
                    start.localScale = TargetTransform.localScale;
                }
            }

            // set new destination in any case. new data is best data.
            goal = temp;
        }

        // try to estimate movement speed for a data point based on how far it moved since the previous one
        // - if this is the first time ever then we use our best guess:
        //     - delta based on transform.localPosition
        //     - elapsed based on send interval hoping that it roughly matches
        private static float EstimateMovementSpeed(DataPoint from, DataPoint to, Transform transform,
            float sendInterval)
        {
            var delta = to.localPosition - (from.localPosition != transform.localPosition
                ? from.localPosition
                : transform.localPosition);
            var elapsed = from.IsValid ? to.timeStamp - from.timeStamp : sendInterval;

            // avoid NaN
            return elapsed > 0 ? delta.magnitude / elapsed : 0;
        }

        // set position carefully depending on the target component
        private void ApplyPositionRotationScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // local position/rotation for VR support
            if (syncPosition) TargetTransform.localPosition = position;
            if (syncRotation) TargetTransform.localRotation = rotation;
            if (syncScale) TargetTransform.localScale = scale;
        }

        // where are we in the timeline between start and goal? [0,1]
        private Vector3 InterpolatePosition(DataPoint start, DataPoint goal, Vector3 currentPosition)
        {
            if (!interpolatePosition)
                return currentPosition;

            if (start.movementSpeed != 0)
            {
                // Option 1: simply interpolate based on time, but stutter will happen, it's not that smooth.
                // This is especially noticeable if the camera automatically follows the player
                // -         Tell SonarCloud this isn't really commented code but actual comments and to stfu about it
                // -         float t = CurrentInterpolationFactor();
                // -         return Vector3.Lerp(start.position, goal.position, t);

                // Option 2: always += speed
                // speed is 0 if we just started after idle, so always use max for best results
                var speed = Mathf.Max(start.movementSpeed, goal.movementSpeed);
                return Vector3.MoveTowards(currentPosition, goal.localPosition, speed * Time.deltaTime);
            }

            return currentPosition;
        }

        private Quaternion InterpolateRotation(DataPoint start, DataPoint goal, Quaternion defaultRotation)
        {
            if (!interpolateRotation)
                return defaultRotation;

            if (start.localRotation != goal.localRotation)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Quaternion.Slerp(start.localRotation, goal.localRotation, t);
            }

            return defaultRotation;
        }

        private Vector3 InterpolateScale(DataPoint start, DataPoint goal, Vector3 currentScale)
        {
            if (!interpolateScale)
                return currentScale;

            if (start.localScale != goal.localScale)
            {
                var t = CurrentInterpolationFactor(start, goal);
                return Vector3.Lerp(start.localScale, goal.localScale, t);
            }

            return currentScale;
        }

        private static float CurrentInterpolationFactor(DataPoint start, DataPoint goal)
        {
            if (start.IsValid)
            {
                var difference = goal.timeStamp - start.timeStamp;

                // the moment we get 'goal', 'start' is supposed to start, so elapsed time is based on:
                var elapsed = Time.time - goal.timeStamp;

                // avoid NaN
                return difference > 0 ? elapsed / difference : 1;
            }

            return 1;
        }

        #region Debug Gizmos

        // draw the data points for easier debugging
        private void OnDrawGizmos()
        {
            // draw start and goal points and a line between them
            if (start.localPosition != goal.localPosition)
            {
                DrawDataPointGizmo(start, Color.yellow);
                DrawDataPointGizmo(goal, Color.green);
                DrawLineBetweenDataPoints(start, goal, Color.cyan);
            }
        }

        private static void DrawDataPointGizmo(DataPoint data, Color color)
        {
            // use a little offset because transform.localPosition might be in the ground in many cases
            var offset = Vector3.up * 0.01f;

            // draw position
            Gizmos.color = color;
            Gizmos.DrawSphere(data.localPosition + offset, 0.5f);

            // draw forward and up like unity move tool
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.forward);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.up);
        }

        private static void DrawLineBetweenDataPoints(DataPoint data1, DataPoint data2, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(data1.localPosition, data2.localPosition);
        }

        #endregion
    }
}