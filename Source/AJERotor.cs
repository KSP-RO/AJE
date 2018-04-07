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

        [KSPField]
        public bool useOxygen = true;
        [KSPField]
        public float IspMultiplier = 1f;
        [KSPField]
        public float idle = 0f;
        [KSPField]
        public float rpm;
        [KSPField]
        public float r;
        [KSPField]
        public float weight;
        [KSPField]
        public float power;
        [KSPField]
        public float BSFC;
        [KSPField]
        public float VTOLbuff = -1f;
        [KSPField]
        public float maxSwashPlateAngle = 20f;
        [KSPField]
        public int clockWise = -1;
        #endregion
        #region Control/Display fields

        [KSPField]
        public bool yaw = false;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Yaw Coeff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.1f)]
        public float yawCoeff = 0f;
        [KSPField]
        public bool colDiffPitch = false;//for Chinook
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Pitch ctrl by Collective Diff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.05f)]
        public float colDiffPitchCoeff = 0f;
        [KSPField]
        public bool rollDiffYaw = false;//for Chinook
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Yaw ctrl by Roll Diff.", guiFormat = "0.##")
            , UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.05f)]
        public float rollDiffYawCoeff = 0f;      
        [KSPField]
        public bool colDiffRoll = false;//for Osprey
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Roll ctrl by Collective Diff.", guiFormat = "0.##")
            , UI_FloatRange(affectSymCounterparts = UI_Scene.None, minValue = -1f, maxValue = 1f, stepIncrement = 0.05f)]
        public float colDiffRollCoeff = 0f;
        [KSPField]
        public bool pitchDiffYaw = false;//for Osprey
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Yaw ctrl by Pitch Diff.", guiFormat = "0.##")
            , UI_FloatRange(affectSymCounterparts = UI_Scene.None, minValue = -1f, maxValue = 1f, stepIncrement = 0.05f)]
        public float pitchDiffYawCoeff = 0f;

        [KSPField]
        public bool reversible = false;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Reverse Rotation", isPersistant = true)
            , UI_Toggle(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.Editor)]
        public bool reverseRotation = false;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Cyclic Control", isPersistant = true)
            , UI_Toggle(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All)]
        public bool cycEnabled = true;
        [KSPAction("Toggle Cyclic Control")]
        public void toggleCyclicAction(KSPActionParam param)
        {
            toggleCyclic();
        }
        public void toggleCyclic()
        {
            cycEnabled = !cycEnabled;
        }
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Kp", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float rollKp = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Ki", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float rollKi = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Kd", guiFormat = "0.##")
             , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float rollKd = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Kp", guiFormat = "0.##")
            , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float pitchKp = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Ki", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float pitchKi = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Kd", guiFormat = "0.##")
           , UI_FloatRange(minValue = 0, maxValue = 3f, stepIncrement = 0.01f)]
        public float pitchKd = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cyclic Trim to Speed", guiFormat = "0.##")
         , UI_FloatRange(minValue = -2f, maxValue = 2f, stepIncrement = 0.1f)]
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

        public void SetFields()
        {
            Fields["yawCoeff"].guiActive = Fields["yawCoeff"].guiActiveEditor = yaw;
            Fields["colDiffPitchCoeff"].guiActive = Fields["colDiffPitchCoeff"].guiActiveEditor = colDiffPitch;
            Fields["rollDiffYawCoeff"].guiActive = Fields["rollDiffYawCoeff"].guiActiveEditor = rollDiffYaw;
            Fields["colDiffRollCoeff"].guiActive = Fields["colDiffRollCoeff"].guiActiveEditor = colDiffRoll;
            Fields["pitchDiffYawCoeff"].guiActive = Fields["pitchDiffYawCoeff"].guiActiveEditor = pitchDiffYaw;
            Fields["reverseRotation"].guiActive = Fields["reverseRotation"].guiActiveEditor = reversible;

        }
        public override void CreateEngine()
        {
            if (maxEngineTemp == 0d)
                maxEngineTemp = part.maxTemp;

            float omega = rpm * 0.1047f;

            engineSolver = new SolverRotor(omega, r, weight, power * 745.7f, 1.2f, VTOLbuff, BSFC, useOxygen, reverseRotation ? -clockWise : clockWise);


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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            SetFields();
        }
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
                radar = (float)vessel.radarAltitude + Vector3.Dot(vessel.upAxis, thrustTransforms[0].position - vessel.transform.position);
                if (cycEnabled)
                {
                    choppercontrol.x = vessel.ctrlState.roll; 
                    choppercontrol.y = vessel.ctrlState.pitch;
                    choppercontrol.y -= Vector3.Dot(vel, hdg) * cycTrim * 0.01f;
                    choppercontrol.z = vessel.ctrlState.mainThrottle;// -  * vessel.ctrlState.pitch - colDiffRollCoeff * vessel.ctrlState.roll;
                    
                    //thrustTransforms[0].forward = Quaternion.AngleAxis(-choppercontrol.x * maxSwashPlateAngle, hdg) * thrustTransformVectorDefault;
                    //thrustTransforms[0].forward = Quaternion.AngleAxis(choppercontrol.y * maxSwashPlateAngle, rgt) * thrustTransforms[0].forward;

                    float rollangle = (float)Vector3d.Angle(rgt, vessel.upAxis) - 90f;
                    float pitchangle = 90f - (float)Vector3d.Angle(hdg, vessel.upAxis);
                    
                    rollPID.Update(-rollKp, -rollKi, -rollKd, choppercontrol.x * 60, rollangle, Time.deltaTime);
                    pitchPID.Update(-pitchKp, -pitchKi, -pitchKd, choppercontrol.y * 60, pitchangle, Time.deltaTime);

                    if (colDiffRoll)//Osprey
                    {
                        choppercontrol.z += rollPID.getDrive() / 100 * colDiffRollCoeff;
                        choppercontrol.x = 0;
                    }
                    else
                        choppercontrol.x = rollPID.getDrive() / 100 + rollDiffYawCoeff * vessel.ctrlState.yaw;

                    if (colDiffPitch)//Chinook
                    {
                        choppercontrol.z += pitchPID.getDrive() / 100 * colDiffPitchCoeff;
                        choppercontrol.y = Vector3.Dot(vel, hdg) * cycTrim * 0.01f; 
                    }
                    else
                        choppercontrol.y = pitchPID.getDrive() / 100 + pitchDiffYawCoeff * vessel.ctrlState.yaw;

                    

                }
                else
                {
                    choppercontrol.x = choppercontrol.y = 0;
                    choppercontrol.z = vessel.ctrlState.mainThrottle;
                }
            }
            else
            {
                hdg.Set(0, 1, 0);
            }

            Vector3 t = thrustTransforms[0].forward.normalized;
            (engineSolver as SolverRotor).UpdateFlightParams(choppercontrol, vel, hdg, t, radar, (float)ambientTherm.SpeedOfSound(1), this.thrustPercentage);

            base.UpdateSolver(ambientTherm, altitude, vel, mach, ignited, oxygen, underwater);
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();

            ShaftPower = (float)(engineSolver as SolverRotor).GetPower() / 745.7f;
            RPM = (engineSolver as SolverRotor).omega / 0.1047f;

            Vector3 F = (engineSolver as SolverRotor).Force;
            part.Rigidbody.AddForce(Vector3.ProjectOnPlane(F, thrustTransforms[0].forward.normalized) * 0.001f);
            part.Rigidbody.AddTorque((engineSolver as SolverRotor).Tilt * 0.001f);
            if (clockWise == 0)
            {
                part.Rigidbody.AddTorque(thrustTransforms[0].forward.normalized * (engineSolver as SolverRotor).shaftpower / (engineSolver as SolverRotor).omega * yawCoeff * -0.05f * vessel.ctrlState.yaw);
            }
            else
            {
                part.Rigidbody.AddTorque((engineSolver as SolverRotor).Torque * yawCoeff * -0.05f * vessel.ctrlState.yaw);
            }
           /* Vector3 L = (float)(engineSolver as SolverRotor).thrust * thrustTransforms[0].forward.normalized;

            if(pitchDiffYaw)
                part.Rigidbody.AddForce(hdg * Vector3.Project(L, dwn).magnitude * 1.74524E-5f * pitchDiffYawCoeff * vessel.ctrlState.yaw);
            if(rollDiffYaw)
                part.Rigidbody.AddForce(rgt * Vector3.Project(L, dwn).magnitude * 1.74524E-5f * rollDiffYawCoeff * vessel.ctrlState.yaw); */
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
