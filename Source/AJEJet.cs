using System;
using UnityEngine;
using SolverEngines;
using SolverEngines.EngineFitting;

namespace AJE
{

    public class ModuleEnginesAJEJet : ModuleEnginesSolver, IModuleInfo, IEngineStatus, IFittableEngine, AnimationModules.INozzleArea, AnimationModules.IJetAfterburner
    {
        [EngineFitResult]
        [KSPField(isPersistant = false, guiActive = false)]
        public float Area = 0.1f;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float BPR = 0;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float CPR = 20;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float FPR = 1;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float Mdes = 0.9f;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float Tdes = 250;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_c = 0.95f;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_t = 0.98f;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_n = 0.9f;
        [EngineFitResult]
        [KSPField(isPersistant = false, guiActive = false)]
        public float FHV = 46.8E6f;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float TIT = 1200;
        [EngineFitResult]
        [KSPField(isPersistant = false, guiActive = false)]
        public float TAB = 0;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public bool exhaustMixer = false;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public bool adjustableNozzle = true;
        [EngineParameter]
        [KSPField(isPersistant = false, guiActive = false)]
        public float defaultTPR = 1f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float maxT3 = 9999;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool intakeMatchArea = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public float areaFudgeFactor = 0.75f;

        [EngineFitResult]
        public float minThrottle = 0.01f;
        [EngineFitResult]
        public float turbineAreaRatio = 0.75f;

        [EngineFitData]
        [KSPField(isPersistant = false, guiActive = false)]
        public float drySFC = 0f;
        [EngineFitData]
        [KSPField(isPersistant = false, guiActive = false)]
        public float dryThrust = 0f;
        [EngineFitData]
        [KSPField(isPersistant = false, guiActive = false)]
        public float wetThrust = 0f;
        [EngineFitData]
        [KSPField(isPersistant = false, guiActive = false)]
        public float idleNPR = 1.1f;

        [KSPField(isPersistant = false, guiActive = false)]
        public string spoolEffectName2 = "spool2";
        [KSPField(isPersistant = false, guiActive = false)]
        public string powerEffectName2 = "power2";

        [KSPField]
        public float throttleResponseMultiplier = 1f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Compression Ratio", guiFormat = "F1")]
        public float prat3 = 0f;

#if DEBUG
        [KSPField(guiActive = true, guiName = "Nozzle Area", guiFormat = "F2", guiUnits = "m^2")]
        public float nozzleArea;

        [KSPField(guiActive = true, guiName = "Exhaust Temperature", guiFormat = "F1", guiUnits = "K")]
        public float exhaustTemp;
#endif

        private SolverJet solverJet;

        public override void CreateEngine()
        {
            //           bool DREactive = AssemblyLoader.loadedAssemblies.Any(
            //               a => a.assembly.GetName().Name.Equals("DeadlyReentry.dll", StringComparison.InvariantCultureIgnoreCase));
            //         heatProduction = (float)part.maxTemp * 0.1f;
            engineSolver = solverJet = new SolverJet();
            solverJet.InitializeOverallEngineData(
                Area,
                BPR,
                CPR,
                FPR,
                Mdes,
                Tdes,
                eta_c,
                eta_t,
                eta_n,
                FHV,
                TIT,
                TAB,
                minThrottle,
                turbineAreaRatio,
                exhaustMixer,
                adjustableNozzle
                );
            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;
            maxEngineTemp = maxT3;
            if (autoignitionTemp < 0f || float.IsInfinity(autoignitionTemp))
                autoignitionTemp = 500f; // Autoignition of Kerosene is 493.15K

            PushAreaToInlet();

            // set heat production
            heatProduction = Mathf.Min(10f, (1f - eta_c) * (1f - eta_t) * (1f - eta_n) * (10000f + TAB * 10f) / (1f + BPR * 0.5f));
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
            double requiredThrottle = requestedThrottle * thrustPercentage * 0.01d;
            double deltaT = TimeWarp.fixedDeltaTime;
            double throttleResponseRate = Math.Max(1 / (1.28 * Area * (1 + BPR) + 3.22), 0.1) * throttleResponseMultiplier;

            double d = requiredThrottle - currentThrottle;
            if (Math.Abs(d) > throttleResponseRate * deltaT)
                currentThrottle += Mathf.Sign((float)d) * (float)(throttleResponseRate * deltaT);
            else
                currentThrottle = (float)requiredThrottle;

            base.UpdateThrottle();
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();
            prat3 = (float)solverJet.GetPrat3();

#if DEBUG
            nozzleArea = GetNozzleArea();
            exhaustTemp = GetEmissiveTemp();
#endif
        }

        public override float RequiredIntakeArea()
        {
            return base.RequiredIntakeArea() * areaFudgeFactor;
        }

        public override void FXUpdate()
        {
            base.FXUpdate();
            
            part.Effect(spoolEffectName2, engineSolver.GetFXSpool());
            part.Effect(powerEffectName2, engineSolver.GetFXPower());
        }

        public override void DeactivatePowerFX()
        {
            base.DeactivatePowerFX();

            part.Effect(powerEffectName2, 0f);
        }

        public override void DeactivateLoopingFX()
        {
            base.DeactivateLoopingFX();

            part.Effect(spoolEffectName2, 0f);
        }

        public bool Afterburning => (TAB > 0f);

        public float GetEmissiveTemp()
        {
            if (isOperational)
                return (float)solverJet.GetExhaustTemp();
            else
                return (float)part.temperature;
        }

        public float GetNozzleArea()
        {
            if (isOperational)
                return (float)solverJet.GetNozzleArea();
            else
                return 0f;
        }

        public float GetCoreThrottle()
        {
            if (isOperational)
                return (float)solverJet.GetCoreThrottle();
            else
                return 0f;
        }

        public float GetABThrottle()
        {
            if (isOperational)
                return (float)solverJet.GetABThrottle();
            else
                return 0f;
        }

        public float GetStaticDryNozzleArea()
        {
            SetStaticSimulation();
            currentThrottle = FullDryThrottle;
            UpdateSolver(ambientTherm, 0d, Vector3d.zero, 0d, true, true, false);

            return (float)solverJet.GetNozzleArea();
        }

        public float GetStaticWetNozzleArea()
        {
            SetStaticSimulation();
            currentThrottle = 1f;
            UpdateSolver(ambientTherm, 0d, Vector3d.zero, 0d, true, true, false);

            return (float)solverJet.GetNozzleArea();
        }

        #region Engine Fitting

        public bool CanFitEngine => true;

        public void PushFitParamsToSolver()
        {
            solverJet.SetFitParams(Area, FHV, TAB, minThrottle, turbineAreaRatio);
            PushAreaToInlet();
        }

        public void DoEngineFit()
        {
            SolverJet jetEngine = engineSolver as SolverJet;
            jetEngine.FitEngine(dryThrust * 1000d, drySFC, wetThrust * 1000d, idleNPR, defaultTPR : defaultTPR);

            Area = (float)jetEngine.GetAref();
            FHV = (float)jetEngine.GetFHV();
            TAB = (float)jetEngine.GetTAB();
            minThrottle = (float)jetEngine.GetMinThrottle();
            turbineAreaRatio = (float)jetEngine.GetTurbineAreaRatio();

            PushAreaToInlet();
        }

        protected void PushAreaToInlet()
        {
            if (intakeMatchArea)
            {
                AJEInlet intake = part.FindModuleImplementing<AJEInlet>();
                if (intake != null)
                    intake.Area = RequiredIntakeArea();
            }
        }

        #endregion

        #region Info

        public string GetStaticThrustInfo(bool primaryField)
        {
            string output = "";

            SetStaticSimulation();
            currentThrottle = 1f;

            UpdateSolver(ambientTherm, 0d, Vector3d.zero, 0d, true, true, false);
            double thrust = (engineSolver.GetThrust() * 0.001d);
            
            if (Afterburning)
            {
                output += "<b>Static Thrust (wet): </b>" + thrust.ToString("N2") + " kN";
                if (!primaryField)
                    output += "\n   <b>SFC: </b>" + engineSolver.GetSFC().ToString("N4") + " kg/kgf-h";
                currentThrottle = 2f / 3f;
                UpdateSolver(ambientTherm, 0d, Vector3d.zero, 0d, true, true, false);
                thrust = (engineSolver.GetThrust() * 0.001d);
                output += "\n<b>Static Thrust (dry): </b>" + thrust.ToString("N2") + " kN";
                if (!primaryField)
                    output += "\n   <b>SFC: </b>" + engineSolver.GetSFC().ToString("N4") + " kg/kgf-h\n";
            }
            else
            {
                output += "<b>Static Thrust: </b>" + thrust.ToString("N2") + " kN";
                if (!primaryField)
                    output += "\n   <b>SFC: </b>" + engineSolver.GetSFC().ToString("N4") + " kg/kgf-h\n";
            }

            if (!primaryField)
            {
                output += "\n<b>Required Area:</b> " + RequiredIntakeArea().ToString("F3") + " m^2";
                if (BPR > 0f)
                    output += "\n<b>Bypass Ratio:</b> " + BPR.ToString("F2");
                output += "\n<b>Compression Ratio (static):</b> " + solverJet.GetPrat3().ToString("F1") + "\n";
            }
            
            return output;
        }

        public override string GetModuleTitle() => (BPR > 0) ? "AJE Turbofan" : "AJE Turbojet";
        public override string GetPrimaryField() => GetStaticThrustInfo(true);
        
        public override string GetInfo()
        {
            string output = GetStaticThrustInfo(false);

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

            currentThrottle = 0f;

            return output;
        }

        #endregion

        private void SetStaticSimulation()
        {
            ambientTherm = EngineThermodynamics.StandardConditions(true);
            
            inletTherm = ambientTherm;
            inletTherm.P *= AJEInlet.OverallStaticTPR(defaultTPR);

            areaRatio = 1d;
            lastPropellantFraction = 1d;
        }
        
        private float FullDryThrottle => Afterburning ? (2f / 3f) : 1f;
    }
}

