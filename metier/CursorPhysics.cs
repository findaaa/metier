#nullable disable
using System;
using System.Drawing;

namespace eep.editer1
{
    public class CursorPhysics
    {
        private const float Y_SMOOTH = 0.3f;
        private const float X_TENSION = 0.15f;
        private const float RAPID_TENSION = 0.02f;
        private const float RAPID_FRICTION = 0.85f;
        private const float FRICTION_FORWARD = 0.65f;
        private const float FRICTION_BACKWARD = 0.45f;
        private const float SNAP_THRESHOLD = 0.5f;
        private const float STOP_VELOCITY = 0.5f;

        public float PosX { get; private set; }
        public float PosY { get; private set; }
        private float velX = 0;
        private float maxTargetX = 0;

        public void Update(Point realTargetPos, bool isTyping, bool isDeleting, float ratchetThreshold, float deltaTime, float charWidthLimit, bool isComposing, long elapsedInput)
        {
            float effectiveTargetX = realTargetPos.X;

            if (isTyping && !isDeleting)
            {
                if (realTargetPos.X >= maxTargetX)
                {
                    maxTargetX = realTargetPos.X;
                    effectiveTargetX = realTargetPos.X;
                }
                else
                {
                    float jumpDistance = maxTargetX - realTargetPos.X;
                    if (jumpDistance < ratchetThreshold) effectiveTargetX = maxTargetX;
                    else
                    {
                        maxTargetX = realTargetPos.X;
                        effectiveTargetX = realTargetPos.X;
                    }
                }
            }
            else
            {
                maxTargetX = realTargetPos.X;
                effectiveTargetX = realTargetPos.X;
            }

            PosY += (realTargetPos.Y - PosY) * Y_SMOOTH * deltaTime;

            float diffX = effectiveTargetX - PosX;
            float diffY = Math.Abs(realTargetPos.Y - PosY);

            if (diffY > 5.0f)
            {
                PosX += diffX * 0.3f * deltaTime;
                velX = 0;
            }
            else if (Math.Abs(diffX) < SNAP_THRESHOLD && Math.Abs(velX) < STOP_VELOCITY)
            {
                PosX = effectiveTargetX;
                velX = 0;
            }
            else
            {
                float tension;
                float friction;
                bool isMovingLeft = (diffX < 0);

                if (isTyping && !isMovingLeft)
                {
                    tension = RAPID_TENSION;
                    friction = RAPID_FRICTION;
                }
                else if (isMovingLeft)
                {
                    tension = X_TENSION;
                    friction = FRICTION_BACKWARD;
                }
                else
                {
                    tension = X_TENSION;
                    friction = FRICTION_FORWARD;
                }

                float force = diffX * tension;
                velX += force * deltaTime;
                velX *= (float)Math.Pow(friction, deltaTime);
                PosX += velX * deltaTime;
            }
        }
    }
}