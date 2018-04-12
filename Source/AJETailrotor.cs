using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;
using SolverEngines;

namespace AJE
{
    public class AJETailrotor : PartModule
    {
        [KSPField]
        public bool useOxygen = true;
        [KSPField]
        public string thrustVectorTransformName = "thrustTransform";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Torque Multiplier", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.05f)]
        public float torqueMultiplier=1f;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Matching Rotor RPM")]
        public float rpm;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Distance to Rotor Axis (m)")]
        public float r;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Matching Max Power (HP)")]
        public float power;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Kp", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2.5f, stepIncrement = 0.05f)]
        public float yawKp = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Ki", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2.5f, stepIncrement = 0.05f)]
        public float yawKi = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Kd", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2.5f, stepIncrement = 0.05f)]
        public float yawKd = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Tail Rotor active", isPersistant = true)
            , UI_Toggle()]
        public bool tailrotorActive = true;
        [KSPAction("toggle Tail Rotor")]
        public void toggleTailRotor(KSPActionParam param)
        {
            tailrotorActive = !tailrotorActive;
        }
        Transform t;
        float takeoffLift;
        SmoothInput smoothinput;       
        PIDController yawPID1, yawPID2;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor)
                return;
            if (vessel == null)
                return;

            float omega = rpm * 0.1047f;
            takeoffLift = power * 745.7f / omega / r; //in newton
            yawPID1 = new PIDController((int)(1 / Time.fixedDeltaTime));
            yawPID2 = new PIDController((int)(1 / Time.fixedDeltaTime));
            t = part.FindModelTransform(thrustVectorTransformName);
            smoothinput = new SmoothInput(0.5f);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (!tailrotorActive)
                return;
            if (vessel == null)
                return;
            if ((!vessel.mainBody.atmosphereContainsOxygen && useOxygen) || part.vessel.altitude > vessel.mainBody.atmosphereDepth)
            {
                return;
            }
            if (t == null)
                return;

            float yaw = vessel.ctrlState.yaw;
            Vector3 dwn = vessel.ReferenceTransform.forward;
            Vector3 rgt = vessel.ReferenceTransform.right;
            Vector3 hdg = vessel.ReferenceTransform.up;
        
            float currYawSpeed = Vector3.Dot(vessel.angularVelocity, dwn);
            float currYawAngle = -Vector3.SignedAngle(Vector3.ProjectOnPlane(vessel.srf_vel_direction, dwn), hdg, dwn);
            float sign = Mathf.Sign(Vector3.Dot(t.forward, rgt));
            
            yawPID1.Update(-yawKp, -yawKi, -yawKd, yaw, currYawSpeed, Time.deltaTime);
            yawPID2.Update(-yawKp, -yawKi, -yawKd, yaw, currYawAngle/30, Time.deltaTime);

            float input;
            float spd = Vector3.Dot(vessel.srf_velocity, hdg);
            float y1 = 5 * sign * yawPID1.getDrive() * (1 + vessel.ctrlState.mainThrottle) / 2;
            float y2 = 2 * sign * yawPID2.getDrive();
            if (spd < 0)
                input = y1;
            else if (spd > 20)
                input = y2;
            else
                input = (spd) * y2 / 20 + (20 - spd) * y1 / 20;

            input = Mathf.Clamp(smoothinput.Smooth(input), 0, 1.5f);
            float maxLift = takeoffLift * torqueMultiplier;

            float lift = input * maxLift;
                
            
            Vector3 r = t.position - vessel.transform.position;
            Vector3 V = vessel.srf_velocity + Vector3.Cross(vessel.angularVelocity, r);        
            float vx = Vector3.ProjectOnPlane(V, t.forward.normalized).magnitude;
            float vz = -Vector3.Dot(V, t.forward.normalized);

            //effective translational lift
            if (vx > 10)
                lift *= 1.3f;
            else if (vx > 5)
                lift *= 0.3f / 5 * (vx - 5) + 1;
            //happens at 10-20kn

            if (Mathf.Abs(vz) < 50)
                lift *= 1 - 0.02f * vz;
            else if (vz < 0)
                lift *= 2;
            else
                lift = 0;
            

            part.AddForceAtPosition(lift * t.forward.normalized * 0.001f, t.position);

            //TODO: switch, 
        }

    
    }

    public class SmoothInput
    {
        float currInput,step;
        public SmoothInput(float tau) //tau is the time it takes for the input to increase 1. in second
        {
            step = Time.fixedDeltaTime / tau;
            currInput = 0;
        }

        public float Smooth(float input)
        {
            if (Mathf.Abs(input - currInput) > step)
            {
                if (input > currInput)
                    currInput += step;
                else
                    currInput -= step;
            }
            else
            {
                currInput = input;
            }
            return currInput;
        }
    }
}
