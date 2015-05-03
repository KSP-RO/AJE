using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{
    public class AJEInlet : ModuleResourceIntake
    {
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public float Area;
        [KSPField(isPersistant = false, guiActive = false)]
        public FloatCurve TPRCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false)]
        public bool useTPRCurve = true;
        [KSPField(isPersistant = false, guiActive = true)]
        public double cosine = 1d;

        private Transform intakeTransform = null; // replaces original

        public float GetTPR(float Mach)
        {
            if (useTPRCurve)
            {
                return TPRCurve.Evaluate(Mach);
            }
            else
            {
                if(Mach<=1f)
                    return 1f;
                else
                    return 1.0f - .075f * Mathf.Pow(Mach - 1.0f, 1.35f); 
            }

        }
                
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (intakeTransform == null)
            {
                intakeTransform = part.FindModelTransform(intakeTransformName);
            }
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (vessel.mainBody.atmosphereContainsOxygen == false || part.vessel.altitude > vessel.mainBody.atmosphereDepth)
            {
                return;
            }

            if (!intakeEnabled) //by Virindi
            {
                cosine = 1f;
            }
            else
            {

                double realcos = Math.Max(0d, Vector3.Dot(vessel.srf_velocity.normalized, intakeTransform.forward));

                double fakecos = (-0.000123d * vessel.srfSpeed * vessel.srfSpeed + 0.002469d * vessel.srfSpeed + 0.987654d);
                if (fakecos > 1d)
                    fakecos = 1d;

                cosine = Math.Max(realcos, fakecos); //by Virindi
            }
        }

    }

}
