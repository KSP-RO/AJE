using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;
using SolverEngines;

namespace AJE
{
    public class ModuleEnginesAJEPropeller : ModuleEnginesSolver, IModuleInfo
    {
        #region Fields
        #region Loadable fields
        // engine fields
        [KSPField]
        public bool useOxygen = true;
        [KSPField]
        public double minRPM;
        [KSPField]
        public double maxRPM;
        [KSPField]
        public double power;
        [KSPField]
        public double gearratio;
        [KSPField]
        public bool turbo = false;
        [KSPField]
        public double BSFC = 7.62e-08d;
        [KSPField]
        public double wastegateMP = 29.921d;
        [KSPField]
        public double compression = 7d;
        [KSPField]
        public double displacement = -1d;
        [KSPField]
        public double boost0 = -1d;
        [KSPField]
        public double rated0 = -1d;
        [KSPField]
        public double boost1 = -1d;
        [KSPField]
        public double rated1 = -1d;
        [KSPField]
        public double cost1 = 0d;
        [KSPField]
        public double switchAlt = 180d;
        [KSPField]
        public double exhaustThrust = 0d;
        [KSPField]
        public double meredithEffect = 0.0d;
        [KSPField]
        public double coolerEffic = 0d;
        [KSPField]
        public double coolerMin = -200f;
        [KSPField]
        public double ramAir = 0.2f;

        // Prop fields
        [KSPField(isPersistant = true, guiActive = false)]
        public string propName;
        [KSPField(isPersistant = true, guiActive = false)]
        public double propDiam = -1d;
        [KSPField(isPersistant = true, guiActive = false)]
        public double propIxx = -1d;
        #endregion

        #region Control/Display fields
        [KSPField(guiActive = true, guiName = "Prop RPM", guiFormat = "0.##")]
        public float propRPM = 0f;

        [KSPField(guiActive = true, guiName = "Prop Pitch", guiUnits = " deg.")]
        public float propPitch = 0.0f;

        [KSPField(guiActive = true, guiName = "Prop Thrust", guiFormat = "0.###", guiUnits = " kN")]
        public float propThrust = 0f;

        [KSPField(guiActive = true, guiName = "Manifold Pressure", guiFormat = "0.###", guiUnits = " inHg")]
        public float manifoldPressure = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge Air Temp", guiFormat = "0.###", guiUnits = " K")]
        public float chargeAirTemp = 288.0f;

        [KSPField(guiActive = true, guiName = "Exhaust Thrust", guiFormat = "0.###", guiUnits = " kN")]
        public float netExhaustThrust = 0.0f;

        [KSPField(guiActive = true, guiName = "Meredith Effect", guiFormat = "0.###", guiUnits = " kN")]
        public float netMeredithEffect = 0.0f;

        [KSPField(guiActive = true, guiName = "Brake Horsepower", guiFormat = "0", guiUnits = " HP")]
        public float brakeHorsepower = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Boost", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float boost = 1.0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "RPM Lever", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float RPM = 1.0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Mixture", guiFormat = "0.###"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.005f)]
        public float mixture = 0.7f; // optimal "auto rich"
        #endregion

        #region Debug tweakables
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "CtTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 2.0f, stepIncrement = 0.01f)]
        public float CtTweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "CpTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 2.0f, stepIncrement = 0.01f)]
        public float CpTweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "VolEFficMult", guiFormat = "0.##"), UI_FloatRange(minValue = 0.25f, maxValue = 1.5f, stepIncrement = 0.01f)]
        public float VolETweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "MachPowTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.5f, stepIncrement = 0.01f)]
        public float MachPowTweak = 1.0f;

        [KSPField]
        public bool debugFields = false;
        #endregion

        #region Internal Fields
        //[KSPField(guiActive = true)]
        public float v;

        public const double INHG2PA = 101325.0d / 760d * 1000d * 0.0254d; // 1 inch of Mercury in Pascals
        public const double PA2INHG = 1d / INHG2PA;
        public const double CTOK = 273.15d;


        //[KSPField(guiActive = true)]
        //public string thrustvector;
        //[KSPField(guiActive = true)]
        //public string velocityvector;

        protected PistonEngine pistonEngine;
        protected SolverPropeller solverProp;
        #endregion
        #endregion

        #region Setup methods
        public override void CreateEngine()
        {
            pistonEngine = null;
            
            double powW = power * PistonEngine.HP2W;
            
            if (maxEngineTemp == 0d)
                maxEngineTemp = 3600d;
            heatProduction = 10f; // HACK fixme. But we don't want to create much heat in the part.

            if (useOxygen)
            {
                pistonEngine = new PistonEngine(
                    powW,
                    maxRPM,
                    BSFC,
                    ramAir,
                    displacement * PistonEngine.CIN2CM,
                    compression,
                    coolerEffic,
                    coolerMin + CTOK,
                    exhaustThrust,
                    meredithEffect,
                    // Super/turbo params:
                    wastegateMP * INHG2PA, boost0 * INHG2PA, boost1 * INHG2PA, rated0, rated1, cost1 * PistonEngine.HP2W, switchAlt, turbo
                    );
            }
            engineSolver = solverProp = new SolverPropeller(pistonEngine, powW, BSFC, gearratio, propName, minRPM * gearratio, maxRPM * gearratio, propDiam, propIxx);

            if (solverProp.GetConstantSpeed() == 0)
                Fields["propPitch"].guiActive = false;
            if (exhaustThrust <= 0f)
                Fields["netExhaustThrust"].guiActive = false;
            if (meredithEffect <= 0f)
                Fields["netMeredithEffect"].guiActive = false;

            Fields["statusL2"].guiActive = true; // always show
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SetFields();
        }
        #endregion

        #region Update methods
        public override void UpdateFlightCondition(EngineThermodynamics ambientTherm, double altitude, double vel, double mach, bool oxygen)
        {
            // change up the velocity vector, it's now vs the engine part.
            vel = Vector3.Dot(vessel.srf_velocity, -thrustTransforms[0].forward.normalized);
            v = (float)vel;

            // set engine params prior to calculation
            if (pistonEngine != null)
            {
                pistonEngine.SetWastegate(boost);
                pistonEngine.SetMixture(mixture);
            }

            base.UpdateFlightCondition(ambientTherm, altitude, vel, mach, oxygen);
        }
        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();
            Fields["statusL2"].guiActive = true; // always show

            brakeHorsepower = (float)(solverProp.GetShaftPower() * PistonEngine.W2HP);
            if(pistonEngine != null)
            {
                manifoldPressure = (float)(pistonEngine.GetMAP() * PA2INHG);
                chargeAirTemp = (float)pistonEngine.GetChargeTemp();
            }
            propRPM = (float)solverProp.GetPropRPM();
            propPitch = (float)solverProp.GetPropPitch();
        }
        #endregion

        #region Info methods
        public string GetBaseInfo()
        {
            string output = "";
            if(useOxygen && boost0 > 1d)
            {
                if(turbo)
                    output += "Turbosupercharged, ";
                else
                    output += "Supercharged, ";
            }
            return output + power + "HP\n";
        }
        public override string GetModuleTitle()
        {
            return (useOxygen ? "AJE Piston Engine" : "AJE Propeller Engine");
        }
        public override string GetPrimaryField()
        {
            return GetBaseInfo();
        }

        public override string GetInfo()
        {
            string output = GetBaseInfo();
            output += minRPM.ToString("N0") + " / " + maxRPM.ToString("N0") + " RPM, gearing " + gearratio.ToString("N3") + "\n";
            if (useOxygen && boost0 > 1d)
            {
                output += "Max MP " + wastegateMP.ToString("N3") +" inHg, Rated: " + boost0.ToString("N2") + "ata at " + rated0.ToString("N1") + "km";
                if (boost1 > 1d)
                {
                    output += ", " + boost1.ToString("N2") + "ata at " + rated1.ToString("N1") + "km";
                    if (switchAlt > 0d)
                        output += ", switch " + switchAlt.ToString("N1") + "km";
                    else
                        output += ", autoswitching";
                }
                output += "\n";
            }
            output += "BSFC: " + BSFC + " kg/W-s\nPropeller: " + solverProp.GetDiameter().ToString("N2") + "m diameter";
            return output;
        }
        #endregion

        #region Helpers
        public void SetFields()
        {
            Fields["engineTempString"].guiName = "Exhaust Temp";

            if (!useOxygen)
            {
                Fields["VolETweak"].guiName = "Power Multiplier";
                Fields["manifoldPressure"].guiActive = Fields["manifoldPressure"].guiActiveEditor =
                    Fields["boost"].guiActive = Fields["boost"].guiActiveEditor =
                    Fields["mixture"].guiActive = Fields["mixture"].guiActiveEditor =
                    Fields["chargeAirTemp"].guiActive = Fields["chargeAirTemp"].guiActiveEditor = false;
            }

            // set debug fields
            Fields["CtTweak"].guiActive = Fields["CtTweak"].guiActiveEditor =
                Fields["CpTweak"].guiActive = Fields["CpTweak"].guiActiveEditor =
                Fields["VolETweak"].guiActive = Fields["VolETweak"].guiActiveEditor =
                Fields["MachPowTweak"].guiActive = Fields["MachPowTweak"].guiActiveEditor = debugFields;
        }
        #endregion
    }
}