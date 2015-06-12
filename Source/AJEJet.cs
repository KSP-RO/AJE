using KSP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using SolverEngines;

namespace AJE
{


    public class ModuleEnginesAJEJet : ModuleEnginesSolver, IModuleInfo
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float Area = 0.1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float BPR = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float CPR = 20;
        [KSPField(isPersistant = false, guiActive = false)]
        public float FPR = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float Mdes = 0.9f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float Tdes = 250;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_c = 0.95f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_t = 0.98f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_n = 0.9f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float FHV = 46.8E6f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TIT = 1200;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TAB = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool exhaustMixer = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxT3 = 9999;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Compression Ratio", guiFormat = "F1")]
        public float prat3 = 0f;

        
        public override void CreateEngine()
        {
            //           bool DREactive = AssemblyLoader.loadedAssemblies.Any(
            //               a => a.assembly.GetName().Name.Equals("DeadlyReentry.dll", StringComparison.InvariantCultureIgnoreCase));
            //         heatProduction = (float)part.maxTemp * 0.1f;
            engineSolver = new SolverJet();
            (engineSolver as SolverJet).InitializeOverallEngineData(
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
                exhaustMixer
                );
            useAtmCurve = atmChangeFlow = useVelCurve = false;
            maxEngineTemp = maxT3;

            if (CPR == 1f)
                Fields["prat3"].guiActive = false;
        }

        public override void UpdateThrottle()
        {
            if (CPR != 1)
            {
                double requiredThrottle = requestedThrottle * thrustPercentage * 0.01d;
                double deltaT = TimeWarp.fixedDeltaTime;
                double throttleResponseRate = Math.Max(2 / Area / (1 + BPR), 5) * 0.01d; //percent per second

                double d = requiredThrottle - currentThrottle;
                if (Math.Abs(d) > throttleResponseRate * deltaT)
                    currentThrottle += Mathf.Sign((float)d) * (float)(throttleResponseRate * deltaT);
                else
                    currentThrottle = (float)requiredThrottle;
            }
            else // ramjet
            {
                currentThrottle = (float)(requestedThrottle * thrustPercentage * 0.01);
            }
            currentThrottle = Mathf.Max(0.01f, currentThrottle);
            base.UpdateThrottle();
        }

        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();
            prat3 = (float)(engineSolver as SolverJet).Prat3;
        }

        public string GetStaticThrustInfo(bool primaryField)
        {
            string output = "";
            if (engineSolver == null || !(engineSolver is SolverJet))
                CreateEngine();

            // get stats
            double pressure = 101.325d, temperature = 288.15d, density = 1.225d;
            if (Planetarium.fetch != null)
            {
                CelestialBody home = Planetarium.fetch.Home;
                if (home != null)
                {
                    pressure = home.GetPressure(0d);
                    temperature = home.GetTemperature(0d);
                    density = home.GetDensity(pressure, temperature);
                }
            }
            ambientTherm = new EngineThermodynamics();
            ambientTherm.FromAmbientConditions(pressure, temperature, density);

            inletTherm = new EngineThermodynamics();
            inletTherm.CopyFrom(ambientTherm);
            inletTherm.P *= 0.987654d * 0.987654d; // Static cosine

            areaRatio = 1d;
            currentThrottle = 1f;
            lastPropellantFraction = 1d;
            bool oldE = EngineIgnited;
            EngineIgnited = true;
            
            UpdateFlightCondition(ambientTherm, 0d, Vector3d.zero, 0d, true);
            double thrust = (engineSolver.GetThrust() * 0.001d);

            if (CPR == 1f) // ramjet
            {
                if (primaryField)
                    output += "<b>Ramjet</b> (no static thrust)\n";
                if (thrustUpperLimit != double.MaxValue)
                    output += "<b>Max Rated Thrust:</b> " + thrustUpperLimit.ToString("N2") + " kN\n";
                if (!primaryField)
                    output += "<b>Area:</b> " + Area + "\n";
            }
            else
            {
                if (TAB == 0f ) // no AB
                {
                    output += "<b>Static Thrust: </b>" + thrust.ToString("N2") + " kN";
                    if (!primaryField)
                        output += "\n   <b>SFC: </b>" + (1d / engineSolver.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h\n";
                }
                else
                {
                    output += "<b>Static Thrust (wet): </b>" + thrust.ToString("N2") + " kN";
                    if (!primaryField)
                        output += "\n   <b>SFC: </b>" + (1d / engineSolver.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h";
                    currentThrottle = 2f / 3f;
                    UpdateFlightCondition(ambientTherm, 0d, Vector3d.zero, 0d, true);
                    thrust = (engineSolver.GetThrust() * 0.001d);
                    output += "\n<b>Static Thrust (dry): </b>" + thrust.ToString("N2") + " kN";
                    if (!primaryField)
                        output += "\n   <b>SFC: </b>" + (1d / engineSolver.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h\n";
                }
            }

            if (!primaryField && CPR != 1f)
            {
                output += "\n<b>Required Area:</b> " + engineSolver.GetArea().ToString("F3") + " m^2";
                if (BPR > 0f)
                    output += "\n<b>Bypass Ratio:</b> " + BPR.ToString("F2");
                output += "\n<b>Compression Ratio (static):</b> " + (engineSolver as SolverJet).Prat3.ToString("F1") + "\n";
            }

            EngineIgnited = oldE;
            return output;
        }
        public override string GetModuleTitle()
        {
            if (CPR == 1)
                return "AJE Ramjet";
            if (BPR > 0)
                return "AJE Turbofan";
            return "AJE Turbojet";
        }
        public override string GetPrimaryField()
        {
            return GetStaticThrustInfo(true);
        }

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
    }
}

