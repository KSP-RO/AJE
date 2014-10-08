using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{
    public class AJEInlet:PartModule
    {
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public float Area;
        [KSPField(isPersistant = false, guiActive = false)]
        public FloatCurve TPRCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false)]
        public bool useTPRCurve = true;
        [KSPField(isPersistant = false, guiActive = true)]
        public float cosine = 1f;
        public ModuleResourceIntake intake;

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
            if (state == StartState.Editor)
                return;
            if (vessel == null)
                return;
            intake=(ModuleResourceIntake)part.Modules["ModuleResourceIntake"];
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (vessel.mainBody.atmosphereContainsOxygen == false || part.vessel.altitude > vessel.mainBody.maxAtmosphereAltitude)
            {
                return;
            }
            if (part.Modules.Contains("ModuleResourceIntake"))
            {
                if (!intake.intakeEnabled) //by Virindi
                {
                    cosine = 1f;
                }
                else
                {

                    float realcos = Mathf.Max(0f, Vector3.Dot(vessel.srf_velocity.normalized, part.FindModelTransform(intake.intakeTransformName).forward.normalized));

                    float fakecos = (float)(-0.000123d * vessel.srfSpeed * vessel.srfSpeed + 0.002469d * vessel.srfSpeed + 0.987654d);
                    if (fakecos > 1f)
                        fakecos = 1f;

                    cosine = Mathf.Max(realcos, fakecos); //by Virindi
                }
            }
        }

    }

}
