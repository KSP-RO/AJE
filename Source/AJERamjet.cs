using UnityEngine;
using SolverEngines;
using System;

namespace AJE
{
    public class ModuleEnginesAJERamjet : ModuleEnginesSolver, IModuleInfo, IEngineStatus, AnimationModules.INozzleArea, AnimationModules.IJetAfterburner
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float Area = 0.1f;
        
        [KSPField(isPersistant = false, guiActive = false)]
        public float Mdes = 0.9f;
        
        [KSPField(isPersistant = false, guiActive = false)]
        public float Tdes = 250;

        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_n = 0.9f;
        
        [KSPField(isPersistant = false, guiActive = false)]
        public float FHV = 46.8E6f;
        
        [KSPField(isPersistant = false, guiActive = false)]
        public bool adjustableNozzle = true;

        [KSPField(isPersistant = false, guiActive = false)]
        public float defaultTPR = 1f;

        // Max fuel to air ratio - calculated assuming combustion of dodecane and that air is 23.2% oxygen by mass
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxFar = 0.06676f;
        
#if DEBUG
        [KSPField(guiActive = true, guiName = "Nozzle Area", guiFormat = "F2", guiUnits = "m^2")]
        public float nozzleArea;

        [KSPField(guiActive = true, guiName = "Exhaust Temperature", guiFormat = "F1", guiUnits = "K")]
        public float exhaustTemp;
#endif

        private SolverRamjet solverJet;

        public override void CreateEngine()
        {
            //           bool DREactive = AssemblyLoader.loadedAssemblies.Any(
            //               a => a.assembly.GetName().Name.Equals("DeadlyReentry.dll", StringComparison.InvariantCultureIgnoreCase));
            //         heatProduction = (float)part.maxTemp * 0.1f;
            engineSolver = solverJet = new SolverRamjet();
            solverJet.InitializeOverallEngineData(
                Area,
                Mdes,
                Tdes,
                eta_n,
                FHV,
                maxFar,
                adjustableNozzle
                );
            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;
            if (maxEngineTemp <= 0f) maxEngineTemp = 4000f;
            if (autoignitionTemp < 0f || float.IsInfinity(autoignitionTemp))
                autoignitionTemp = 500f; // Autoignition of Kerosene is 493.15K
        }

        public override void CreateEngineIfNecessary()
        {
            if (engineSolver == null || !(engineSolver is SolverJet))
                CreateEngine();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            currentThrottle = 0.01f;
            base.UpdateThrottle();
        }

        public override void UpdateThrottle()
        {
            if (EngineIgnited && !flameout)
            {
                currentThrottle = (float)(requestedThrottle * thrustPercentage * 0.01);
                currentThrottle = Mathf.Max(0.01f, currentThrottle);
            }
            else
            {
                currentThrottle = 0f;
            }
            base.UpdateThrottle();
        }

#if DEBUG
        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();
        
            nozzleArea = GetNozzleArea();
            exhaustTemp = GetEmissiveTemp();
        }
#endif

        public float GetEmissiveTemp() => isOperational ? (float)solverJet.GetExhaustTemp() : (float)part.temperature;
        public float GetNozzleArea() => isOperational ? (float)solverJet.GetNozzleArea() : 0f;
        public float GetABThrottle() => isOperational ? currentThrottle : 0f;

        #region Info
        public override string GetModuleTitle()
        {
            return "AJE Ramjet";
        }

        public override string GetPrimaryField()
        {
            string output = "<b>Ramjet</b> (no static thrust)\n";
            output += "<b>Max Rated Thrust:</b> " + thrustUpperLimit.ToString("N2") + " kN\n";
            return output;
        }

        public override string GetInfo()
        {
            string output = "<b>Max Rated Thrust:</b> " + thrustUpperLimit.ToString("N2") + " kN\n";
            output += "<b>Required Area:</b> " + Area + " m^2\n";

            output += "\n<b><color=#99ff00ff>Propellants:</color></b>\n";
            Propellant p;
            string pName;
            for (int i = 0; i < propellants.Count; ++i)
            {
                p = propellants[i];
                pName = KSPUtil.PrintModuleName(p.name);

                output += "- <b>" + pName + "</b>: " + getMaxFuelFlow(p).ToString("0.0##") + "/sec. Max.\n";
                output += p.GetFlowModeDescription();
            }
            output += "<b>Flameout under: </b>" + (ignitionThreshold * 100f).ToString("0.#") + "%\n";

            if (!allowShutdown) output += "\n" + "<b><color=orange>Engine cannot be shut down!</color></b>";
            if (!allowRestart) output += "\n" + "<b><color=orange>If shutdown, engine cannot restart.</color></b>";

            return output;
        }

        #endregion
    }
}
