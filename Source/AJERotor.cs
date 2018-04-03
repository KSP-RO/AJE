using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;
using SolverEngines;

namespace AJE
{
    public class ModuleEnginesAJERotor : ModuleEnginesSolver, IModuleInfo, IEngineStatus
    {
        #region Fields

        #region Loadable Fields

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
        public float VTOLbuff = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxSwashPlateAngle = 20f;
        #endregion
        #region Control/Display fields

        [KSPField(isPersistant = false, guiActive = false)]
        public bool yaw = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Yaw Coeff.", guiFormat = "0.##"), UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float yawCoeff = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool colDiffPitch = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Collective Diff. Pitch Coeff.", guiFormat = "0.##"), UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float colDiffPitchCoeff = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool cycDiffYaw = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Cyclic Diff. Coeff.", guiFormat = "0.##"), UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float cycDiffYawCoeff = 0f;

        #endregion
        #region Display Fields

        [KSPField(isPersistant = false, guiActive = true, guiName = "Shaft Power", guiFormat = "F0", guiUnits = "HP")]
        public float ShaftPower;

        #endregion

        #region Internal Fields

        Vector3 thrustTransformVectorDefault;
        #endregion

        #endregion

        #region Setup Methods


        public override void CreateEngine()
        {
            if (maxEngineTemp == 0d)
                maxEngineTemp = part.maxTemp;

            float omega = rpm * 0.1047f;

            engineSolver = new SolverRotor(omega, r, weight, power * 745.7f, 1.2f, VTOLbuff, BSFC, useOxygen);
     
            
            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;

            thrustTransformVectorDefault = thrustTransforms[0].forward;

            this.Fields["Yaw Coeff."].guiActive = yaw;
            this.Fields["Yaw Coeff."].guiActiveEditor = yaw;
            this.Fields["Collective Diff. Pitch Coeff."].guiActive = colDiffPitch;
            this.Fields["Collective Diff. Pitch Coeff."].guiActiveEditor = colDiffPitch;
            this.Fields["Cyclic Diff. Coeff."].guiActive = cycDiffYaw;
            this.Fields["Cyclic Diff. Coeff."].guiActiveEditor = cycDiffYaw;
        }

        #endregion

        #region Update Methods

        public override void UpdateThrottle()
        {
            currentThrottle = requestedThrottle * thrustPercentage * 0.01f; // instant throttle response
            base.UpdateThrottle();
        }
        Vector3 choppercontrol = Vector3.zero;
        public override void UpdateSolver(EngineThermodynamics ambientTherm, double altitude, Vector3d vel, double mach, bool ignited, bool oxygen, bool underwater)
        {
            choppercontrol=Vector3.zero;
            Vector3 hdg=Vector3.zero;
            float radar = 0; 
            if (HighLogic.LoadedSceneIsEditor)
            {
                hdg = vessel.ReferenceTransform.up;
                choppercontrol.z = 0.85f;
            }
            else if(HighLogic.LoadedSceneIsFlight)
            {
                hdg = vessel.ReferenceTransform.up;
                Vector3 rgt = vessel.ReferenceTransform.right;
                Vector3d dwn = vessel.ReferenceTransform.forward;
                choppercontrol.x = vessel.ctrlState.roll * (1 - cycDiffYawCoeff * 0.01f * vessel.ctrlState.yaw);
                choppercontrol.y = -vessel.ctrlState.pitch ;
                choppercontrol.z = vessel.ctrlState.mainThrottle * this.thrustPercentage / 100f * (1 + colDiffPitchCoeff * 0.01f * vessel.ctrlState.pitch);
                radar = (float)vessel.radarAltitude;
                thrustTransforms[0].forward = Quaternion.AngleAxis(-choppercontrol.x * maxSwashPlateAngle, hdg) * thrustTransformVectorDefault;
                thrustTransforms[0].forward = Quaternion.AngleAxis(choppercontrol.y * maxSwashPlateAngle, rgt) * thrustTransforms[0].forward;

            }
            else
            {
                hdg.Set(0, 1, 0);
            }

            Vector3 t = thrustTransforms[0].forward.normalized;
            (engineSolver as SolverRotor).UpdateFlightParams(choppercontrol, vel, hdg, t, radar);

            base.UpdateSolver(ambientTherm, altitude, vel, mach, ignited, oxygen, underwater);
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();

            ShaftPower = (float)(engineSolver as SolverRotor).GetPower() / 745.7f;

            part.Rigidbody.AddForce((engineSolver as SolverRotor).Drag*0.001f);
            part.Rigidbody.AddTorque((engineSolver as SolverRotor).Torque * yawCoeff * -0.01f * vessel.ctrlState.yaw);

        }

        #endregion

        #region Info Methods

        public override string GetModuleTitle()
        {
            return "AJE Rotor";
        }

        public string GetStaticThrustInfo()
        {
            string output = "";
            if (engineSolver == null || !(engineSolver is SolverRotor))
                CreateEngine();

       
            output += "<b>Static Power: </b>" + this.power.ToString("F0") + " HP\n";
            output += "<b>Static Thrust: </b>" + (this.weight*0.009801f).ToString("F2") + " kN\n";

            return output;
        }

        public override string GetInfo()
        {
            return GetStaticThrustInfo();
        }

        public override string GetPrimaryField()
        {
            return GetStaticThrustInfo();
        }

        #endregion
    }
}
