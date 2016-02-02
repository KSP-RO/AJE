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

        #endregion

        #region Display Fields

        [KSPField(isPersistant = false, guiActive = true, guiName = "Shaft Power", guiFormat = "F0", guiUnits = "HP")]
        public float ShaftPower;

        #endregion

        #region Internal Fields

        protected ModuleReactionWheel sas;
        protected float sasP, sasY, sasR;
        protected float vx, vz;

        #endregion

        #endregion

        #region Setup Methods

        public override void CreateEngine()
        {
            if (maxEngineTemp == 0d)
                maxEngineTemp = part.maxTemp;

            float omega = rpm * 0.1047f;
//            power *= 745.7f;
            engineSolver = new SolverRotor(omega, r, weight, power * 745.7f, 1.2f, VTOLbuff, BSFC, useOxygen);
            sas = part.FindModuleImplementing<ModuleReactionWheel>();
            if (sas != null)
            {
                sasP = sas.PitchTorque;
                sasY = sas.YawTorque;
                sasR = sas.RollTorque;
            }

            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;
        }

        #endregion

        #region Update Methods

        public override void UpdateThrottle()
        {
            currentThrottle = requestedThrottle; // instant throttle response
            base.UpdateThrottle();
        }

        public override void UpdateSolver(EngineThermodynamics ambientTherm, double altitude, Vector3d vel, double mach, bool ignited, bool oxygen, bool underwater)
        {
            Vector3 t = thrustTransforms[0].forward.normalized;

            vx = (float)Vector3.Cross(vel, t).magnitude;
            vz = -(float)Vector3.Dot(vel, t);

            (engineSolver as SolverRotor).UpdateFlightParams(vx, vz);

            base.UpdateSolver(ambientTherm, altitude, vel, mach, ignited, oxygen, underwater);
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();

            ShaftPower = (float)(engineSolver as SolverRotor).GetPower() / 745.7f;

            if (sas != null)
            {
                float sasMultiplier = (float)(engineSolver as SolverRotor).SASMultiplier();
                if (float.IsNaN(sasMultiplier) || float.IsInfinity(sasMultiplier))
                {
                    Debug.LogError(this.GetType().Name + " on part " + part.partInfo.name + ": invalid SAS multiplier encountered: " + sasMultiplier.ToString());
                }
                else
                {
                    sas.PitchTorque = sasP * sasMultiplier;
                    sas.YawTorque = sasY * sasMultiplier;
                    sas.RollTorque = sasR * sasMultiplier;
                }
            }
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

            // get stats
            ambientTherm = new EngineThermodynamics();
            ambientTherm.FromStandardConditions(true);

            currentThrottle = 1f;
            lastPropellantFraction = 1d;

            UpdateSolver(ambientTherm, 0d, Vector3.zero, 0d, true, true, false);
            double thrust = (engineSolver.GetThrust() * 0.001d);
            double power = ((engineSolver as SolverRotor).GetPower() / 745.7d);

            output += "<b>Static Power: </b>" + power.ToString("F0") + " HP\n";
            output += "<b>Static Thrust: </b>" + thrust.ToString("F2") + " kN\n";

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
