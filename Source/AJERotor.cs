using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{
    public class AJERotor:PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public bool useOxygen = true;
        [KSPField(isPersistant = false, guiActive = false)]
        public float IspMultiplier = 1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float idle = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float rpm;
        [KSPField(isPersistant = false, guiActive = false)]
        public float r;
        [KSPField(isPersistant = false, guiActive = false)]
        public float weight;
        [KSPField(isPersistant = false, guiActive = false)]
        public float power;
        [KSPField(isPersistant = false, guiActive = false)]
        public float BSFC;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxThrust = 99999;
        [KSPField(isPersistant = false, guiActive = false)]
        public float VTOLbuff = -1f;


 //       [KSPField(isPersistant = false, guiActive = true)]
        public float vx;
  //      [KSPField(isPersistant = false, guiActive = true)]
        public float vz;
        [KSPField(isPersistant = false, guiActive = true)]
        public string ShaftPower;


        public EngineWrapper engine;
        public AJERotorSolver aje;
        public ModuleReactionWheel sas;
        public float sasP,sasY,sasR;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor)
                return;
            if (vessel == null)
                return;
            engine = new EngineWrapper(part);
            engine.IspMultiplier = IspMultiplier;
            engine.idle = idle;
            engine.useVelocityCurve = false;
            engine.ThrustUpperLimit = maxThrust;
            float omega = rpm * 0.1047f;
//            power *= 745.7f;
            aje = new AJERotorSolver(omega, r, weight, power * 745.7f, 1.2f, VTOLbuff);
            sas = (ModuleReactionWheel)part.Modules["ModuleReactionWheel"];
            sasP = sas.PitchTorque;
            sasY = sas.YawTorque;
            sasR = sas.RollTorque;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (engine.type == EngineWrapper.EngineType.NONE || !engine.EngineIgnited)
                return;
            if ((!vessel.mainBody.atmosphereContainsOxygen && useOxygen) || part.vessel.altitude > vessel.mainBody.maxAtmosphereAltitude)
            {
                engine.SetThrust(0);
                return;
            }

            
            Vector3d V = vessel.srf_velocity;
            Vector3d t = part.FindModelTransform(engine.thrustVectorTransformName).forward.normalized;

            vx = (float)Vector3d.Cross(V, t).magnitude;
            vz = -(float)Vector3d.Dot(V, t);



            float density = (float)ferram4.FARAeroUtil.GetCurrentDensity(part.vessel.mainBody, (float)part.vessel.altitude);

            aje.calc(density, vx, vz, weight*9.801f);


            engine.SetThrust(aje.lift / 1000f);
            float isp = aje.lift / 9.801f / BSFC / aje.power;
            engine.SetIsp(isp);

            ShaftPower = ((int)(aje.power / 745.7f)).ToString() + "HP";

            if (sas != null)
            {
                float sasMultiplier = aje.lift / weight / 9.801f;
                if (sasMultiplier < 0)
                    sasMultiplier = 0;
                sas.PitchTorque = sasP * sasMultiplier;
                sas.YawTorque = sasY * sasMultiplier;
                sas.RollTorque = sasR * sasMultiplier;
            }
        }







    }

    public class AJERotorSolver
    {
        public float omega, r, weight, power0, rho0;
        public float lift, power, tilt;
        public float ldr, torque;
        public float buff;
      
        const float PI = 3.1416f;

        public AJERotorSolver(float omega, float r, float weight, float power0, float rho0, float buff)
        {
            this.omega = omega;
            this.r = r;
            this.weight = weight;
            this.power0 = power0;
            this.rho0 = rho0;
            this.buff = buff;

            ldr = 3 * r * 9.801f * weight * omega / 4 / power0;
        }

        public void calc(float rho, float vx, float vz, float gravity)
        {
            power = power0 * Mathf.Min(1, rho / rho0);

            float x = vz * gravity / power;
            torque = power / omega / Mathf.Pow((x / buff + 1), buff);

            float CLift = omega * omega * r * r * r / 3 + vx * vx * r / 4 / PI;

            float CTorq = omega * omega * r * r * r * r / 4 + vx * vx * r * r / 8 / PI;

            //          float CTilt = omega * r * r * r * vx / PI + r * vx * vx * vx / omega;

            lift = torque * ldr * CLift / CTorq;
            //           tilt = lift / CLift * CTilt;
        
      
                

        }



    }


}
