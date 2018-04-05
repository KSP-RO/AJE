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

        [KSPField]
        public bool yaw = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Yaw Coeff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float yawCoeff = 0f;
        [KSPField]
        public bool colDiffPitch = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Collective Diff. Pitch Coeff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float colDiffPitchCoeff = 0f;
        [KSPField]
        public bool cycDiffYaw = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Cyclic Diff. Coeff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -10f, maxValue = 10f, stepIncrement = 0.01f)]
        public float cycDiffYawCoeff = 0f;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Kp", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float rollKp = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Ki", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float rollKi = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Kd", guiFormat = "0.##")
             , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float rollKd = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Kp", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float pitchKp = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Ki", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float pitchKi = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Kd", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.01f)]
        public float pitchKd = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cyclic Trim to Speed", guiFormat = "0.##")
         , UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.01f)]
        public float cycTrim = 0f;

        #endregion
        #region Display Fields

        [KSPField(isPersistant = false, guiActive = true, guiName = "Shaft Power", guiFormat = "F0", guiUnits = "HP")]
        public float ShaftPower;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Rotor RPM", guiFormat = "0.#", guiUnits = "")]
        public float RPM;
        #endregion

        #region Internal Fields

        Vector3 thrustTransformVectorDefault;
        #endregion

        #endregion

        #region Setup Methods

        PIDController rollPID, pitchPID;

        public override void CreateEngine()
        {
            if (maxEngineTemp == 0d)
                maxEngineTemp = part.maxTemp;

            float omega = rpm * 0.1047f;

            engineSolver = new SolverRotor(omega, r, weight, power * 745.7f, 1.2f, VTOLbuff, BSFC, useOxygen);


            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;

            thrustTransformVectorDefault = thrustTransforms[0].forward;

            /*this.Fields["Yaw Coeff."].guiActive = yaw;
            this.Fields["Yaw Coeff."].guiActiveEditor = yaw;
            this.Fields["Collective Diff. Pitch Coeff."].guiActive = colDiffPitch;
            this.Fields["Collective Diff. Pitch Coeff."].guiActiveEditor = colDiffPitch;
            this.Fields["Cyclic Diff. Coeff."].guiActive = cycDiffYaw;
            this.Fields["Cyclic Diff. Coeff."].guiActiveEditor = cycDiffYaw;*/
            rollPID = new PIDController((int)(1 / Time.fixedDeltaTime));
            pitchPID = new PIDController((int)(1 / Time.fixedDeltaTime));
        }

        #endregion

        #region Update Methods

        public override void UpdateThrottle()
        {
            currentThrottle = requestedThrottle * thrustPercentage * 0.01f; // instant throttle response
            base.UpdateThrottle();
        }
        Vector3 choppercontrol = Vector3.zero;
        Vector3 hdg = Vector3.zero;
        Vector3 rgt = Vector3.zero;
        Vector3 dwn = Vector3.zero;

        public override void UpdateSolver(EngineThermodynamics ambientTherm, double altitude, Vector3d vel, double mach, bool ignited, bool oxygen, bool underwater)
        {
            choppercontrol = Vector3.zero;
            float radar = 0;
            if (HighLogic.LoadedSceneIsEditor)
            {
                hdg = vessel.ReferenceTransform.up;
                choppercontrol.z = 0.85f;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                hdg = vessel.ReferenceTransform.up;
                rgt = vessel.ReferenceTransform.right;
                dwn = vessel.ReferenceTransform.forward;
                choppercontrol.x = vessel.ctrlState.roll * (1 - cycDiffYawCoeff * 0.01f * vessel.ctrlState.yaw);
                choppercontrol.x += Vector3.Dot(vel, hdg) * cycTrim / 100f;
                choppercontrol.y = vessel.ctrlState.pitch;
                choppercontrol.z = vessel.ctrlState.mainThrottle * this.thrustPercentage / 100f * (1 - colDiffPitchCoeff * 0.01f * vessel.ctrlState.pitch);
                radar = (float)vessel.radarAltitude;
                //thrustTransforms[0].forward = Quaternion.AngleAxis(-choppercontrol.x * maxSwashPlateAngle, hdg) * thrustTransformVectorDefault;
                //thrustTransforms[0].forward = Quaternion.AngleAxis(choppercontrol.y * maxSwashPlateAngle, rgt) * thrustTransforms[0].forward;

                float rollangle = (float)Vector3d.Angle(rgt, vessel.upAxis) - 90f;
                float pitchangle = 90f - (float)Vector3d.Angle(hdg, vessel.upAxis);

                rollPID.Update(rollKp, rollKi, rollKd, choppercontrol.x*45, rollangle, Time.deltaTime);
                pitchPID.Update(pitchKp, pitchKi, pitchKd, choppercontrol.y*45, pitchangle, Time.deltaTime);
                choppercontrol.x = rollPID.getDrive() / 100 ;
                choppercontrol.y = pitchPID.getDrive() / 100 ;
            }
            else
            {
                hdg.Set(0, 1, 0);
            }

            Vector3 t = thrustTransforms[0].forward.normalized;
            (engineSolver as SolverRotor).UpdateFlightParams(choppercontrol, vel, hdg, t, radar, (float)ambientTherm.SpeedOfSound(1));

            base.UpdateSolver(ambientTherm, altitude, vel, mach, ignited, oxygen, underwater);
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();

            ShaftPower = (float)(engineSolver as SolverRotor).GetPower() / 745.7f;
            RPM = (engineSolver as SolverRotor).omega / 0.1047f;
            part.Rigidbody.AddForce((engineSolver as SolverRotor).Drag * 0.001f);
            part.Rigidbody.AddTorque((engineSolver as SolverRotor).Torque * yawCoeff * -0.01f * vessel.ctrlState.yaw);
            part.Rigidbody.AddTorque((engineSolver as SolverRotor).Tilt * -0.001f);
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
            output += "<b>Static Thrust: </b>" + (this.weight * 0.009801f).ToString("F2") + " kN\n";

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
