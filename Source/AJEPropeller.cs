using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;
using SolverEngines;

namespace AJE
{
    public class ModuleEnginesAJEPropeller : ModuleEnginesSolver, IModuleInfo, IEngineStatus
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
        [KSPField]
        public bool useInHg = true;
        [KSPField]
        public bool useHP = true;

        // Prop fields
        [KSPField(isPersistant = true, guiActive = false)]
        public string propName;
        [KSPField(isPersistant = true, guiActive = false)]
        public double propDiam = -1d;
        [KSPField(isPersistant = true, guiActive = false)]
        public double propIxx = -1d;
        #endregion

        #region Control/Display fields
        [KSPField(guiActive = true, guiName = "Prop RPM", guiFormat = "N1")]
        public float propRPM = 0f;

        [KSPField(guiActive = true, guiName = "Prop Pitch", guiFormat = "N1", guiUnits = " deg.")]
        public float propPitch = 0.0f;

        [KSPField(guiActive = true, guiName = "Prop Thrust", guiFormat = "N3", guiUnits = " kN")]
        public float propThrust = 0f;

        [KSPField(guiActive = true, guiName = "Manifold Pressure", guiFormat = "N3", guiUnits = "inHg")]
        public float manifoldPressure = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge Air Temp", guiFormat = "N2", guiUnits = " K")]
        public float chargeAirTemp = 288.0f;

        [KSPField(guiActive = true, guiName = "Exhaust Thrust", guiFormat = "N5", guiUnits = " kN")]
        public float netExhaustThrust = 0.0f;

        [KSPField(guiActive = true, guiName = "Meredith Effect", guiFormat = "N5", guiUnits = " kN")]
        public float netMeredithEffect = 0.0f;

        [KSPField(guiActive = true, guiName = "Brake Shaft Power", guiFormat = "N0", guiUnits = "HP")]
        public float brakeShaftPower = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Boost", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float boost = 1.0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "RPM Lever", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float rpmLever = 1.0f;
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
        public double v;

        public const double INHG2ATA = 0.034342; // 1 inch of Mercury in atmosphere-absolute
        public const double INHG2PA = 3386; // 1 inch of Mercury in Pascals
        public const double PA2INHG = 1d / INHG2PA;
        public const double CTOK = 273.15d;


        //[KSPField(guiActive = true)]
        //public string thrustvector;
        //[KSPField(guiActive = true)]
        //public string velocityvector;

        protected PistonEngine pistonEngine;
        protected SolverPropeller solverProp;

        // multipliers
        protected double boostMultiplier;
        protected double boostMultiplierRecip;
        protected double powerMultiplier;
        protected double powerMultiplierRecip;
        protected double displMultiplier;
        protected string powerStr = "HP";
        protected string boostStr = "inHg";
        #endregion
        #endregion

        #region Setup methods
        protected void SetUnits()
        {
            if (useInHg)
            {
                boostMultiplier = INHG2PA;
                boostStr = "inHg";
            }
            else
            {
                boostMultiplier = 101325d; // atm to PA
                boostStr = "ata";
            }
            boostMultiplierRecip = 1d / boostMultiplier;

            if (useHP)
            {
                powerMultiplier = PistonEngine.HP2W;
                powerStr = "HP";
                displMultiplier = PistonEngine.CIN2CM;
            }
            else
            {
                powerMultiplier = PistonEngine.PS2W;
                powerStr = "PS";
                displMultiplier = 0.001d; // liters to cubic meters
            }
            powerMultiplierRecip = 1d / powerMultiplier;
        }
        public override void CreateEngine()
        {
            SetUnits();

            pistonEngine = null;
            
            if (maxEngineTemp == 0d)
                maxEngineTemp = 3600d;
            heatProduction = 10f; // HACK fixme. But we don't want to create much heat in the part.

            double powW = power * powerMultiplier;
            if (useOxygen)
            {
                pistonEngine = new PistonEngine(
                    powW,
                    maxRPM,
                    BSFC,
                    ramAir,
                    displacement * displMultiplier,
                    compression,
                    coolerEffic,
                    coolerMin + CTOK,
                    exhaustThrust,
                    meredithEffect,
                    // Super/turbo params:
                    wastegateMP * boostMultiplier, boost0 * boostMultiplier, boost1 * boostMultiplier, rated0, rated1, cost1 * powerMultiplier, switchAlt, turbo
                    );

                if (autoignitionTemp < 0f || float.IsInfinity(autoignitionTemp))
                    autoignitionTemp = 560f; // Approximate for gasoline
            }
            else
            {
                if (autoignitionTemp < 0f || float.IsInfinity(autoignitionTemp))
                    autoignitionTemp = 0f;
            }
            engineSolver = solverProp = new SolverPropeller(pistonEngine, powW, BSFC, gearratio, propName, minRPM * gearratio, maxRPM * gearratio, propDiam, propIxx);

            if (solverProp.GetConstantSpeed() == 0)
                Fields["propPitch"].guiActive = false;
            if (exhaustThrust <= 0f)
                Fields["netExhaustThrust"].guiActive = false;
            if (meredithEffect <= 0f)
                Fields["netMeredithEffect"].guiActive = false;

            Fields["statusL2"].guiActive = true; // always show

            useAtmCurve = atmChangeFlow = useVelCurve = useAtmCurveIsp = useVelCurveIsp = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SetFields();
        }
        #endregion

        #region Update methods
        public override void UpdateThrottle()
        {
            currentThrottle = requestedThrottle; // instant throttle response
            base.UpdateThrottle();
        }
        public override void UpdateFlightCondition(EngineThermodynamics ambientTherm, double altitude, Vector3d vel, double mach, bool oxygen, bool underwater)
        {
            // change up the velocity vector, it's now vs the engine part.
            v = Vector3d.Dot(vel, -thrustTransforms[0].forward);
            vel = (Vector3d)thrustTransforms[0].forward * -v;

            // set engine params prior to calculation
            if (pistonEngine != null)
            {
                pistonEngine.SetWastegate(boost);
                pistonEngine.SetMixture(mixture);
            }
            // set prop (+ engine) params
            solverProp.SetRPMLever(rpmLever);
            solverProp.SetTweaks(CtTweak, CpTweak, VolETweak, MachPowTweak);

            base.UpdateFlightCondition(ambientTherm, altitude, vel, mach, oxygen, underwater);
        }
        public override void CalculateEngineParams()
        {
            base.CalculateEngineParams();
            
            Fields["statusL2"].guiActive = true; // always show
            statusL2 = solverProp.GetStatus();

            brakeShaftPower = (float)(solverProp.GetShaftPower() * powerMultiplierRecip);
            if(pistonEngine != null)
            {
                manifoldPressure = (float)(pistonEngine.GetMAP() * boostMultiplierRecip);
                chargeAirTemp = (float)pistonEngine.GetChargeTemp();
                netExhaustThrust = (float)(pistonEngine._netExhaustThrust * 0.001d);
                netMeredithEffect = (float)(pistonEngine._netMeredithEffect * 0.001d);
            }
            propRPM = (float)solverProp.GetPropRPM();
            propPitch = (float)solverProp.GetPropPitch();
            propThrust = (float)(solverProp.GetPropThrust() * 0.001d);
        }
        #endregion

        #region Info methods
        public string GetBaseInfo()
        {
            SetUnits();

            string output = "";

            if(useOxygen && boost0 > 1d)
            {
                if(turbo)
                    output += "Turbosupercharged, ";
                else
                    output += "Supercharged, ";
            }
            return output + power + powerStr + "\n";
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
            CreateEngine();
            string output = GetBaseInfo();

            output += minRPM.ToString("N0") + " / " + maxRPM.ToString("N0") + " RPM, gearing " + gearratio.ToString("N3") + "\n";

            if (useOxygen && boost0 > 1d)
            {
                double ratingMult = 1d;
                if(!useInHg)
                {
                    boostStr = "ata";
                }
                output += "Max MP " + (wastegateMP * ratingMult).ToString("N3") + boostStr
                    + "\nRated: " + boost0.ToString("N2") + boostStr + " at " + (rated0 * .001d).ToString("N1") + "km";
                if (boost1 > 1d)
                {
                    output += " (1)\nRated: " + (boost1 * ratingMult).ToString("N2") + boostStr + " at " + (rated1 * .001d).ToString("N1") + "km , cost " + cost1.ToString("N0") + powerStr + " (2)";
                    if (switchAlt > 0d)
                        output += "\nSwitching at " + (switchAlt * 0.001d).ToString("N1") + "km\n";
                    else
                        output += "\nAuto-switching\n";
                }
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

            // set units
            Fields["manifoldPressure"].guiUnits = boostStr;
            Fields["brakeShaftPower"].guiUnits = powerStr;
        }
        #endregion
    }
}