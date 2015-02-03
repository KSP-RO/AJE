using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{

    public class AJEPropeller : PartModule
    {

        [KSPField(isPersistant = true, guiActive = true, guiName = "Heat Mult"), UI_FloatRange(minValue = 20f, maxValue = 150f, stepIncrement = 0.5f)]
        public float HeatConst = 50f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float IspMultiplier = 1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool useOxygen = true;
        [KSPField(isPersistant = false, guiActive = false)]
        public float idle = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float minRPM;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxRPM;
        [KSPField(isPersistant = false, guiActive = false)]
        public float power;
        [KSPField(isPersistant = false, guiActive = false)]
        public float gearratio;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool turbo = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public float BSFC = 7.62e-08f;
        [KSPField(isPersistant = true)]
        public string propName;
        
        
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxThrust = 99999;

        [KSPField(isPersistant = false, guiActive = false)]
        public float wastegateMP = 29.921f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float compression = 7f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float displacement = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float boost0 = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float rated0 = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float boost1 = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float rated1 = -1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float cost1 = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float switchAlt = 180f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float exhaustThrust = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float meredithEffect = 0.0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float coolerEffic = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float coolerMin = -200f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float ramAir = 0.2f;

        [KSPField(isPersistant = true, guiActive = false)]
        public float propDiam = -1f;
        [KSPField(isPersistant = true, guiActive = false)]
        public float propIxx = -1f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Prop RPM", guiFormat = "0.##")]
        public float omega;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Prop Pitch", guiUnits = "deg.")]
        public float propPitch = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Prop Thrust", guiFormat = "0.###", guiUnits = "kN")]
        public float thrust;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Manifold Pressure", guiFormat = "0.###", guiUnits = "inHg")]
        public float manifoldPressure = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Flow", guiFormat = "0.###", guiUnits = "kg/s")]
        public float fuelFlow = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge Air Temp", guiFormat = "0.###", guiUnits = "C")]
        public float chargeAirTemp = 15.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Exhaust Thrust", guiFormat = "0.###", guiUnits = "kN")]
        public float netExhaustThrust = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Meredith Effect", guiFormat = "0.###", guiUnits = "kN")]
        public float netMeredithEffect = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Brake Horsepower", guiFormat = "0", guiUnits = "HP")]
        public float ShaftPower;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Boost", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float boost = 1.0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "RPM Lever", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float RPM = 1.0f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Mixture", guiFormat = "0.###"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.005f)]
        public float mixture = 0.7f; // optimal "auto rich"

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CtTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 2.0f, stepIncrement = 0.01f)]
        public float CtTweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CpTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 2.0f, stepIncrement = 0.01f)]
        public float CpTweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "VolETweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.25f, maxValue = 1.5f, stepIncrement = 0.01f)]
        public float VolETweak = 1.0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "MachPowTweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.5f, stepIncrement = 0.01f)]
        public float MachPowTweak = 1.0f;

        //[KSPField(isPersistant = false, guiActive = true)]
        public double density = 1.225f;
        //[KSPField(isPersistant = false, guiActive = true)]
        public double speedOfSound = 343;
        //[KSPField(isPersistant = false, guiActive = true)]
        public double pressure = 101325f;
        //[KSPField(isPersistant = false, guiActive = true)]
        public float temperature = 15f;
        //[KSPField(isPersistant = false, guiActive = true)]
        public float v;

        //[KSPField(isPersistant = false, guiActive = true)]
        public float isp;

        public const float INHG2PA = 101325.0f / 760f * 1000f * 0.0254f; // 1 inch of Mercury in Pascals
        public const float LBFTOKN = 0.00444822162f; // 1 pound-force in kiloNewtons
        public const float CTOK = 273.15f;


        //[KSPField(isPersistant = false, guiActive = true)]
        //public string thrustvector;
        //[KSPField(isPersistant = false, guiActive = true)]
        //public string velocityvector;

        public AJEPropJSB propJSB;
        public PistonEngine pistonengine;

        public EngineWrapper engine;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor)
                return;
            if (vessel == null)
                return;
            engine = new EngineWrapper(part);
            engine.IspMultiplier = IspMultiplier;
            engine.idle = idle;
            engine.useVelocityCurve = false;
            engine.ThrustUpperLimit = maxThrust;
            engine.useEngineResponseTime = false;

            part.maxTemp = 200f;
            engine.heatProduction = 10f;
            //v0 *= 0.5144f;
            //omega0 *= 0.1047f;
            //power0 *= 745.7f;
            //omega *= 0.1047f;
            //power *= 745.7f;

            //propeller = new AJEPropellerSolver(r0, v0 * 0.5144f, omega0 * PistonEngine.RPM2RADPS, rho0, power0 * PistonEngine.HP2W);
            pistonengine = new PistonEngine(power * PistonEngine.HP2W, maxRPM * PistonEngine.RPM2RADPS, BSFC);
            pistonengine._hasSuper = boost0 > 0;
            pistonengine.setBoostParams(wastegateMP * INHG2PA, boost0 * INHG2PA, boost1 * INHG2PA, rated0, rated1, cost1 * PistonEngine.HP2W, switchAlt, turbo);
            if (displacement > 0)
                pistonengine._displacement = displacement * PistonEngine.CIN2CM;
            pistonengine._compression = compression;
            pistonengine._coolerEffic = coolerEffic;
            pistonengine._coolerMin = coolerMin + CTOK;
            pistonengine._ramAir = ramAir;

            propJSB = new AJEPropJSB(propName, minRPM * gearratio, maxRPM * gearratio, propDiam, propIxx);

            if(propJSB.GetConstantSpeed() == 0)
                Fields["propPitch"].guiActive = false;
            if (exhaustThrust <= 0f)
                Fields["netExhaustThrust"].guiActive = false;
            if (meredithEffect <= 0f)
                Fields["netMeredithEffect"].guiActive = false;

            pistonengine.ComputeVEMultiplier(); // given newly-set stats

            omega = 30; // start slow
        }

        void FixProp() // get prop from prefab
        {
            if(part.partInfo != null && part.partInfo.partPrefab.Modules.Contains("AJEPropeller"))
            {
                AJEPropeller prefab = (AJEPropeller)part.partInfo.partPrefab.Modules["AJEPropeller"];
                propJSB = new AJEPropJSB(prefab.propJSB);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            // for now don't worry about loading persistent PROPELLER data
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (propJSB != null)
            {
                ConfigNode n = new ConfigNode("PROPELLER");
                propJSB.Save(n);
                node.AddNode(n);
            }
        }


        public void FixedUpdate()
        {

            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (engine.type == EngineWrapper.EngineType.NONE || !engine.EngineIgnited)
                return;
            if ((!vessel.mainBody.atmosphereContainsOxygen && useOxygen) || part.vessel.altitude > vessel.mainBody.maxAtmosphereAltitude)
            {
                engine.SetThrust(0);
                return;
            }
            // for RPM handling - bool throttleCut = (object)vessel != null && vessel.ctrlState.mainThrottle <= 0;
            pistonengine._boost = boost;
            pistonengine._mixture = mixture;
            pistonengine._volEfficMult = VolETweak;

            density = ferram4.FARAeroUtil.GetCurrentDensity(part.vessel, out speedOfSound);
            v = Vector3.Dot(vessel.srf_velocity, -part.FindModelTransform(engine.thrustVectorTransformName).forward.normalized);
            pressure = FlightGlobals.getStaticPressure(vessel.altitude, vessel.mainBody) * 101325f; // include dynamic pressure
            float dynPressure = 0.5f * (float)density * v * v;
            temperature = FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + CTOK;
            float acturalThrottle = (int)(vessel.ctrlState.mainThrottle * engine.thrustPercentage) / 100f;
            if (acturalThrottle < 0.1f)
                acturalThrottle = 0.1f;
            pistonengine.calc((float)pressure + dynPressure * ramAir, temperature, omega * PistonEngine.RPM2RADPS / gearratio, acturalThrottle);
            if (!useOxygen)
            {
                pistonengine._power = power * PistonEngine.HP2W * acturalThrottle;
                //pistonengine._torque = power * PistonEngine.HP2W / (omega * PistonEngine.RPM2RADPS);
            }
            double shaftHP = pistonengine._power / PistonEngine.HP2W;
            float totalHP = pistonengine._totalPower / PistonEngine.HP2W;

            propJSB.deltaT = (float)TimeWarp.fixedDeltaTime;
            propJSB.SetAdvance(RPM);
            propJSB.SetTweaks(CtTweak, CpTweak, MachPowTweak);
            thrust = (float)propJSB.Calculate(shaftHP, density, v, speedOfSound);
            omega = (float)propJSB.GetRPM();
            ShaftPower = (float)shaftHP;
            manifoldPressure = pistonengine._mp / INHG2PA;
            fuelFlow = pistonengine._fuelFlow;
            chargeAirTemp = pistonengine._chargeTemp - CTOK;
            propPitch = (float)propJSB.GetPitch();

            // heat from mixture, heat hack for over-RPM, multiply by relative manifold pressure
            float tmpRatio = omega / (maxRPM * gearratio);
            if (tmpRatio > 1)
                tmpRatio *= tmpRatio;
            float mixRatio = (1.5f - mixture);
            mixRatio *= mixRatio;
            mixRatio = (mixRatio + 0.2f) * 1.2f;
            float tempDelta = tmpRatio * mixRatio * manifoldPressure / wastegateMP;
            engine.heatProduction = tempDelta * (1.5f - (float)Math.Max(1.2, 0.1 * dynPressure / totalHP)) * HeatConst; // ram air cooling
            // engine overspeed correction (internal friction at high RPM)
            if (tmpRatio > 1.1)
                omega -= (tmpRatio * tmpRatio * tmpRatio) * maxRPM * gearratio * 0.02f;

            // exhaust thrust, normalized to "10% HP in lbf"
            netExhaustThrust = exhaustThrust * (totalHP * 0.1f * LBFTOKN) * tempDelta;

            // Meredith Effect radiator thrust, scaled by Q and by how hot the engine is running and the ambient temperature
            // scaled from N to kN
            netMeredithEffect = meredithEffect * dynPressure * tempDelta * CTOK / temperature * 0.001f;
            float thrustOut = thrust + netExhaustThrust + netMeredithEffect;

            // set minthrust to maxthrust
            if (acturalThrottle == 0)
                engine.idle = 0f;
            else
                engine.idle = 1.0f;
            // set thrust to something KSP can accept, handle drag
            if (thrustOut < 0.001f)
            {
                float drag = thrustOut - 0.001f;
                thrustOut = 0.001f;
                this.part.rigidbody.AddForceAtPosition(engine.thrustTransforms[0].forward * -drag, engine.thrustTransforms[0].position, ForceMode.Force);
            }
            // thrust in kgf divided by kg mdot
            isp = thrustOut * 1000 / 9.80665f / fuelFlow;
            engine.SetThrust(thrustOut);
            engine.SetIsp(isp);

            //Vector3d v1 = part.FindModelTransform("thrustTransform").forward;
            //v1 = vessel.ReferenceTransform.InverseTransformDirection(v1)*100;
            //Vector3d v2 = vessel.srf_velocity;
            //v2 = vessel.ReferenceTransform.InverseTransformDirection(v2);
            //thrustvector = ((int)v1.x).ToString() + " " + ((int)v1.y).ToString() + " " + ((int)v1.z).ToString() + " " + ((int)v1.magnitude).ToString();
            //velocityvector = ((int)v2.x).ToString() + " " + ((int)v2.y).ToString() + " " + ((int)v2.z).ToString() + " " + ((int)v2.magnitude).ToString();
        }



    }
    public class PistonEngine
    {

        public float _throttle = 1f;
        public bool _starter = false; // true=engaged, false=disengaged
        public int _magnetos = 0; // 0=off, 1=right, 2=left, 3=both
        public float _mixture = 0.836481f; //optimal
        public float _boost;
        public bool _fuel;
        public bool _running = true;
        public float _power0;   // reference power setting
        public float _omega0;   //   "       engine speed
        public float _mixCoeff; // fuel flow per omega at full mixture
        public bool _hasSuper;  // true indicates gear-driven (not turbo)
        public float _turboLag; // turbo lag time in seconds
        public float _charge;   // current {turbo|super}charge multiplier
        public float _chargeTarget;  // eventual charge value
        public float _maxMP;    // static maximum pressure
        public float _wastegate;    // wastegate setting, [0:1]
        public float _displacement; // piston stroke volume
        public float _compression;  // compression ratio (>1)
        public float _minthrottle; // minimum throttle [0:1]
        public float _voleffic; // volumetric efficiency
        public float _volEfficMult; // multiplier to VE, for tweaking.
        public float[] _boostMults;
        public float[] _boostCosts;
        public int _boostMode;
        public float _boostSwitch;
        public float _bsfc;
        public float _coolerEffic;
        public float _coolerMin;
        public float _ramAir;

        // Runtime state/output:
        public float _mp;
        public float _torque;
        public float _fuelFlow;
        public float _egt;
        public float _boostPressure;
        public float _oilTemp;
        public float _oilTempTarget;
        public float _dOilTempdt;
        public float _power;
        public float _chargeTemp;
        public float _totalPower;

        public const float HP2W = 745.699872f;
        public const float CIN2CM = 1.6387064e-5f;
        public const float RPM2RADPS = 0.1047198f;
        public const float RAIR = 287.3f;
        public FloatCurve mixtureEfficiency; //fuel:air -> efficiency

        const float T0 = 288.15f;
        const float P0 = 101325f;

        public void LogVars()
        {
            Debug.Log("Dumping engine status");
            foreach (FieldInfo f in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Debug.Log(f.Name + " = " + f.GetValue(this));
            }
            foreach (FieldInfo f in this.GetType().GetFields())
            {
                Debug.Log(f.Name + " = " + f.GetValue(this));
            }
        }

        public PistonEngine(float power, float speed, float BSFC)
        {
            _boost = 1;
            _running = false;
            _fuel = true;
            _boostPressure = 0;
            _hasSuper = false;
            _boostMults = new float[2];
            _boostCosts = new float[2];
            _boostMults[0] = 1f;
            _boostMults[1] = 1f;
            _boostCosts[0] = 0f;
            _boostCosts[1] = 0f;
            _boostMode = 0;
            _boostSwitch = -1f; // auto
            _coolerEffic = 0f;
            _coolerMin = 0f;
            _ramAir = 0.1f;
            _volEfficMult = 1.0f;

            _oilTemp = 298f;
            _oilTempTarget = _oilTemp;
            _dOilTempdt = 0;

            _bsfc = BSFC;

            _minthrottle = 0.1f;
            _maxMP = 1e6f; // No waste gate on non-turbo engines.
            _wastegate = 1f;
            _charge = 1f;
            _chargeTarget = 1f;
            _turboLag = 2f;

            // set reference conditions
            _power0 = power;
            _omega0 = speed;

            // Guess at reasonable values for these guys.  Displacements run
            // at about 2 cubic inches per horsepower or so, at least for
            // non-turbocharged engines.
            // change to 1.4; supercharging...
            _compression = 8;
            _displacement = power * (1.4f * CIN2CM / HP2W);

            mixtureEfficiency = new FloatCurve();
            ConfigNode node = new ConfigNode("MixtureEfficiency");
            node.AddValue("key", "0.05 0 0 2.09732360097324");
            node.AddValue("key", "0.05137 0.00862 2.09732360097324 164.206349206348");
            node.AddValue("key", "0.05179 0.21552 164.206349206348 35.4900398406375");
            node.AddValue("key", "0.0543 0.48276 35.4900398406375 18.1343042071197");
            node.AddValue("key", "0.05842 0.7069 18.1343042071197 9.17092198581561");
            node.AddValue("key", "0.06312 0.83621 9.17092198581561 5.01693121693122");
            node.AddValue("key", "0.06942 0.93103 5.01693121693122 2.7239336492891");
            node.AddValue("key", "0.07786 1 2.7239336492891 0");
            node.AddValue("key", "0.08845 1 0 -1.3521568627451");
            node.AddValue("key", "0.0927 0.98276 -1.3521568627451 -2.02862745098038");
            node.AddValue("key", "0.09695 0.956895 -2.02862745098038 -2.02862745098041");
            node.AddValue("key", "0.099075 0.9439625 -2.02862745098041 -2.02862745098041");
            node.AddValue("key", "0.1001375 0.93749625 -2.02862745098041 -2.02862745098038");
            node.AddValue("key", "0.1012 0.93103 -2.02862745098038 -5.16579275905118");
            node.AddValue("key", "0.1045375 0.8793075 -5.16579275905118 -5.1657927590512");
            node.AddValue("key", "0.107875 0.827585 -5.1657927590512 -5.16579275905119");
            node.AddValue("key", "0.11455 0.72414 -5.16579275905119 -12.6714082503556");
            node.AddValue("key", "0.12158 0.4569 -12.6714082503556 -26.9723225030083");
            node.AddValue("key", "0.12435 0.23276 -26.9723225030083 -119.364102564103");
            node.AddValue("key", "0.125 0 -119.364102564103 0");
            mixtureEfficiency.Load(node);


            ComputeVEMultiplier(); // compute volumetric efficiency of engine, based on rated power and BSFC.
        }

        // return the fuel:air ratio of the given mixture setting.
        // mix = range [0, 1]
        public float FuelAirRatio(float mix)
        {
            return 0.052f + 0.07028571f * mix; // prevent an AFR too high or low
        }

        // return the relative volumetric efficiency of the engine, given its compression ratio
        // at the ambient pressure pAmb and the manifold absolute pressure MAP
        public float GetPressureVE(float pAmb, float MAP)
        {
            // JSBSim
            float gamma = 1.4f;
            return ((gamma - 1f) / gamma) + (_compression - (pAmb / MAP)) / (gamma * (_compression - 1f));
        }

        // return the charge air temperature after heating and cooling (if any)
        // at given manifold absolute pressure MAP, and given ambient pressure and temp
        // Very simple model: the aftercooler will cool the charge to (cooling efficiency) of
        //  ambient temperature, to a minimum temperature of (cooler min)
        public float GetCAT(float MAP, float pAmb, float tAmb)
        {
            // Air entering the manifold does so rapidly, and thus the
            // pressure change can be assumed to be adiabatic.  Calculate a
            // temperature change, and then apply aftercooling/intercooling (if any)
            float T = tAmb * Mathf.Pow((MAP * MAP) / (pAmb * pAmb), 1f / 7f);
            return Math.Max(_coolerMin, T - (T - tAmb) * _coolerEffic);
        }

        // return the mass airflow through the engine
        // running at given speed in radians/sec, given manifold absolute pressure MAP,
        // given ambient pressure and temperature. Depends on displacement and
        // the volumetric efficiency multiplier.
        public float GetAirflow(float pAmb, float tAmb, float speed, float MAP)
        {
            //from JSBSim
            // air flow
            float swept_volume = (_displacement * speed / RPM2RADPS / 60f) / 2f;
            float v_dot_air = swept_volume * GetPressureVE(pAmb, MAP) * _voleffic * _volEfficMult;

            float rho_air_manifold = MAP / (RAIR * GetCAT(MAP, pAmb, tAmb));
            return v_dot_air * rho_air_manifold;
        }

        // will compute the volumetric efficiency multiplier for the engine
        // given all reference engine stats.
        public void ComputeVEMultiplier()
        {
            _voleffic = 1f; // reset the volumetric efficiency multiplier

            // The idea: since HP will be proportional to fuel flow (* mixture efficiency)
            // we compute the multiplier to airflow needed to yield the desired HP.
            // Assume boost0 at takeoff, up to max MP. We use optimal mixture for power.
            float fuelAirRatio = FuelAirRatio(0.7f);
            float MAP = Math.Min(_maxMP, P0 * _boostMults[0]);
            float m_dot_air = GetAirflow(P0, T0, _omega0, MAP);
            float m_dot_fuel = fuelAirRatio * m_dot_air;
            float power = m_dot_fuel * mixtureEfficiency.Evaluate(fuelAirRatio) / _bsfc;
            _voleffic = _power0 / power;
            float m_dot_air2 = GetAirflow(P0, T0, _omega0, MAP);
            float m_dot_fuel2 = fuelAirRatio * m_dot_air2;
            power = m_dot_fuel2 * mixtureEfficiency.Evaluate(fuelAirRatio) / _bsfc;
            MonoBehaviour.print("*AJE* Setting volumetric efficiency. At SL with MAP " + MAP + ", power = " + power / HP2W + "HP, BSFC = " + _bsfc + ", mda/f = " + m_dot_air2 + "/" + m_dot_fuel2 + ", VE = " + _voleffic + ". Orig a/f: " + m_dot_air + "/" + m_dot_fuel);
        }

        // set boost parameters:
        // maximum MAP, the two boost pressures to maintain, the two rated altitudes (km),
        // the cost for the second boost mode, and the switch altitude (in km), or -1f for auto
        public bool setBoostParams(float wastegate, float boost0, float boost1, float rated0, float rated1, float cost1, float switchAlt, bool turbo)
        {
            bool retVal = false;
            if (boost0 > 0)
            {
                _boostMults[0] = boost0 / ((float)FlightGlobals.getStaticPressure(rated0, FlightGlobals.Bodies[1]) * 101325f);
                _maxMP = wastegate;
                retVal = true;
            }
            if (boost1 > 0)
            {
                _boostMults[1] = boost1 / ((float)FlightGlobals.getStaticPressure(rated1, FlightGlobals.Bodies[1]) * 101325f);
                _boostCosts[1] = cost1;
            }
            else
            {
                _boostMults[1] = 0f;
                _boostCosts[1] = 0f;
            }
            if (switchAlt >= 0f)
                _boostSwitch = (float)FlightGlobals.getStaticPressure(switchAlt, FlightGlobals.Bodies[1]) * 101325f;
            else
                _boostSwitch = switchAlt;
            MonoBehaviour.print("*AJE* Setting boost params. MaxMP = " + wastegate + ", boosts = " + _boostMults[0] + "/" + _boostMults[1] + ", switch " + _boostSwitch + " from " + boost0 + "@" + rated0 + ", " + boost1 + "@" + rated1);
            _hasSuper = !turbo && boost0 > 1.0f;
            return retVal;
        }

        // Gets the target for the [turbo]supercharger
        // takes engine speed, boost mode
        float GetChargeTarget(float speed, int boostMode)
        {
            // Calculate the factor required to modify supercharger output for
            // rpm. Assume that the normalized supercharger output ~= 1 when
            // the engine is at the nominal peak-power rpm.  A power equation
            // of the form (A * B^x * x^C) has been derived empirically from
            // some representative supercharger data.  This provides
            // near-linear output over the normal operating range, with
            // fall-off in the over-speed situation.
            float rpm_norm = (speed / _omega0);
            float A = 1.795206541f;
            float B = 0.55620178f;
            float C = 1.246708471f;
            float rpm_factor = A * Mathf.Pow(B, rpm_norm) * Mathf.Pow(rpm_norm, C);
            return 1f + (_boost * (_boostMults[boostMode] - 1f) * rpm_factor);
        }

        float GetCharge(float target)
        {
            float chg = _charge;
            if (_hasSuper)
            {
                // Superchargers have no lag
                chg = target;
            }
            else //if (!_running)
            {
                // Turbochargers only work well when the engine is actually
                // running.  The 25% number is a guesstimate from Vivian.

                // now clamp near target; this also means clamping going down,
                // which isn't quite right, but eh.
                // Also now use when not running -- the point of this is turbo lag!
                if (chg > 0.95f * target)
                    chg  = target;
                else
                    chg = _charge + (target - _charge) * 0.25f;
            }
            return chg;
        }

        // return the manifold absolute pressure in pascals
        // takes the ambient pressure and the boost mode
        // clamps to [ambient pressure.....wastegate]
        public float CalcMAP(float pAmb, int boostMode, float charge)
        {
            // We need to adjust the minimum manifold pressure to get a
            // reasonable idle speed (a "closed" throttle doesn't suck a total
            // vacuum in real manifolds).  This is a hack.
            float _minMP = (-0.004f * _boostMults[boostMode] * _boost) + _minthrottle;

            float MAP = pAmb * charge;


            // Scale the max MP according to the WASTEGATE control input.  Use
            // the un-supercharged MP as the bottom limit.
            MAP = (float)Math.Min(MAP, Math.Max(_wastegate * _maxMP, pAmb));
            
            // Scale to throttle setting
            //if (_running)
                MAP *= _minMP + (1 - _minMP) * _throttle;

            return MAP;
        }

        // Iteration method
        // Will calculate engine parameters.
        // Takes ambient pressure, temperature, and the engine revolutions in radians/sec
        public void calc(float pAmb, float tAmb, float speed, float throttle)
        {
            _running = true; //_magnetos && _fuel && (speed > 60*RPM2RADPS);
            _throttle = throttle;

            // check if we need to switch boost modes
            float power;
            float MAP;
            float fuelRatio = FuelAirRatio(_mixture);
            if (_boostSwitch > 0)
            {
                if (pAmb < _boostSwitch - 1000 && _boostMode < 1)
                    _boostMode++;
                if (pAmb > _boostSwitch + 1000 && _boostMode > 0)
                    _boostMode--;

                _chargeTarget = GetChargeTarget(speed, _boostMode);
                _charge = GetCharge(_chargeTarget);

                MAP = CalcMAP(pAmb, _boostMode, _charge);

                // Compute fuel flow
                _fuelFlow = GetAirflow(pAmb, tAmb, speed, MAP) * fuelRatio;
                power = _fuelFlow * mixtureEfficiency.Evaluate(fuelRatio) / _bsfc - _boostCosts[_boostMode];
            }
            else // auto switch
            {
                // assume supercharger for now, so charge = target
                float target0 = GetChargeTarget(speed, 0);
                float target1 = GetChargeTarget(speed, 1);
                float charge0 = GetCharge(target0);
                float charge1 = GetCharge(target1);
                float MAP0 = CalcMAP(pAmb, 0, charge0);
                float MAP1 = CalcMAP(pAmb, 1, charge1);

                float m_dot_fuel0 = GetAirflow(pAmb, tAmb, speed, MAP0) * fuelRatio;
                float power0 = m_dot_fuel0 * mixtureEfficiency.Evaluate(fuelRatio) / _bsfc - _boostCosts[0];

                float m_dot_fuel1 = GetAirflow(pAmb, tAmb, speed, MAP1) * fuelRatio;
                float power1 = m_dot_fuel1 * mixtureEfficiency.Evaluate(fuelRatio) / _bsfc - _boostCosts[1];

                if (power0 >= power1)
                {
                    MAP = MAP0;
                    _chargeTarget = target0;
                    _charge = charge0;
                    power = power0;
                    _fuelFlow = m_dot_fuel0;
                    _boostMode = 0;
                }
                else
                {
                    MAP = MAP1;
                    _chargeTarget = target1;
                    _charge = charge1;
                    power = power1;
                    _fuelFlow = m_dot_fuel1;
                    _boostMode = 1;
                }
            }

            _mp = MAP;
            _chargeTemp = GetCAT(MAP, pAmb, tAmb); // duplication of effort, but oh well

            // The "boost" is the delta above ambient
            _boostPressure = _mp - pAmb;

            _power = power;
            _totalPower = _power + _boostCosts[_boostMode];
            _torque = power / speed;

            // Figure that the starter motor produces 15% of the engine's
            // cruise torque.  Assuming 60RPM starter speed vs. 1800RPM cruise
            // speed on a 160HP engine, that comes out to about 160*.15/30 ==
            // 0.8 HP starter motor.  Which sounds about right to me.  I think
            // I've finally got this tuned. :)
            if (_starter && !_running)
                _torque += 0.15f * _power0 / _omega0;

            // Also, add a negative torque of 8% of cruise, to represent
            // internal friction.  Propeller aerodynamic friction is too low
            // at low RPMs to provide a good deceleration.  Interpolate it
            // away as we approach cruise RPMs (full at 50%, zero at 100%),
            // though, to prevent interaction with the power computations.
            // Ugly.
            /*if (speed > 0 && speed < _omega0)
            {
                float interp = 2 - 2 * speed / _omega0;
                interp = (interp > 1) ? 1 : interp;
                _torque -= 0.08f * (_power0 / _omega0) * interp;
            }*/


            // UNUSED
            /*
            // Now EGT.  This one gets a little goofy.  We can calculate the
            // work done by an isentropically expanding exhaust gas as the
            // mass of the gas times the specific heat times the change in
            // temperature.  The mass is just the engine displacement times
            // the manifold density, plus the mass of the fuel, which we know.
            // The change in temperature can be calculated adiabatically as a
            // function of the exhaust gas temperature and the compression
            // ratio (which we know).  So just rearrange the equation to get
            // EGT as a function of engine power.  Cool.  I'm using a value of
            // 1300 J/(kg*K) for the exhaust gas specific heat.  I found this
            // on a web page somewhere; no idea if it's accurate.  Also,
            // remember that four stroke engines do one combustion cycle every
            // TWO revolutions, so the displacement per revolution is half of
            // what we'd expect.  And diddle the work done by the gas a bit to
            // account for non-thermodynamic losses like internal friction;
            // 10% should do it.
            float massFlow = _fuelFlow + (rho * 0.5f * _displacement * speed);
            float specHeat = 1300;
            float corr = 1.0f / (Mathf.Pow(_compression, 0.4f) - 1.0f);
            _egt = corr * (power * 1.1f) / (massFlow * specHeat);
            if (_egt < temp) _egt = temp;


            // Oil temperature.
            // Assume a linear variation between ~90degC at idle and ~120degC
            // at full power.  No attempt to correct for airflow over the
            // engine is made.  Make the time constant to attain target steady-
            // state oil temp greater at engine off than on to reflect no
            // circulation.  Nothing fancy, but populates the guage with a
            // plausible value.
            float tau;	// secs 
            if (_running)
            {
                _oilTempTarget = 363.0f + (30.0f * (power / _power0));
                tau = 600;
                // Reduce tau linearly to 300 at max power
                tau -= (power / _power0) * 300.0f;
            }
            else
            {
                _oilTempTarget = temp;
                tau = 1500;
            }
            _dOilTempdt = (_oilTempTarget - _oilTemp) / tau;
             */
        }

    }

    // largely a port of JSBSim's FGTable
    [Serializable]
    public class FGTable : IConfigNode
    {
        double[,] Data;
        public uint nRows, nCols;
        uint lastRowIndex = 2;
        uint lastColumnIndex = 2;

        public FGTable(FGTable t)
        {
            Data = t.Data;
            nRows = t.nRows;
            nCols = t.nCols;
            lastRowIndex = t.lastRowIndex;
            lastColumnIndex = t.lastColumnIndex;
        }
        public FGTable(ConfigNode node)
        {
            Load(node);
        }
        public void Load(ConfigNode node)
        {
            nRows = (uint)node.values.Count-1;
            if (nRows > 0)
            {
                string[] tmp = node.values[1].value.Split(null);
                nCols = (uint)tmp.Length - 1;
            }
            else
            {
                // then no point to having a table...
                string[] tmp = node.values[0].value.Split(null);
                nCols = (uint)tmp.Length - 1;
            }
            
            Data = new double[nRows + 1, nCols + 1];
            for (int i = 0; i <= nRows; i++)
            {
                string[] curRow = node.values[i].value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < curRow.Length; j++)
                {
                    double dtmp = -999;
                    int offset = 0;
                    if (i == 0)
                        offset = 1;
                    if (double.TryParse(curRow[j], out dtmp))
                        Data[i, j+offset] = dtmp;
                }
            }
        }
        public void Save(ConfigNode node)
        {
            for (uint i = 0; i <= nRows; i++)
            {
                string curRow = "";
                uint max = nCols;
                if (i == 0)
                    max--;
                for (uint j = 0; j < max; j++)
                    curRow += Data[i,j] + " ";
                node.AddValue("key", curRow);
            }
        }
        public double GetValue(double rowKey, double colKey)
        {
            double rFactor, cFactor, col1temp, col2temp, Value;
            uint r = lastRowIndex;
            uint c = lastColumnIndex;

            while (r > 2 && Data[r - 1, 0] > rowKey) { r--; }
            while (r < nRows && Data[r, 0] < rowKey) { r++; }

            while (c > 2 && Data[0, c - 1] > colKey) { c--; }
            while (c < nCols && Data[0, c] < colKey) { c++; }

            lastRowIndex = r;
            lastColumnIndex = c;

            rFactor = (rowKey - Data[r - 1, 0]) / (Data[r, 0] - Data[r - 1, 0]);
            cFactor = (colKey - Data[0, c - 1]) / (Data[0, c] - Data[0, c - 1]);

            // allow off-the-end linear interpolation
            /*if (rFactor > 1.0) rFactor = 1.0;
            else*/ if (rFactor < 0.0) rFactor = 0.0;

            if (cFactor > 1.0) cFactor = 1.0;
            else if (cFactor < 0.0) cFactor = 0.0;

            col1temp = rFactor * (Data[r, c - 1] - Data[r - 1, c - 1]) + Data[r - 1, c - 1];
            col2temp = rFactor * (Data[r, c] - Data[r - 1, c]) + Data[r - 1, c];

            Value = col1temp + cFactor * (col2temp - col1temp);

            return Value;
        }
        double GetValue(double key)
        {
            double Factor, Value, Span;
            uint r = lastRowIndex;

            //if the key is off the end of the table, just return the
            //end-of-table value, do not extrapolate
            if (key <= Data[1, 0])
            {
                lastRowIndex = 2;
                //cout << "Key underneath table: " << key << endl;
                return Data[1, 1];
            }
            else if (key >= Data[nRows, 0])
            {
                lastRowIndex = nRows;
                //cout << "Key over table: " << key << endl;
                return Data[nRows, 1];
            }

            // the key is somewhere in the middle, search for the right breakpoint
            // The search is particularly efficient if 
            // the correct breakpoint has not changed since last frame or
            // has only changed very little

            while (r > 2 && Data[r - 1, 0] > key) { r--; }
            while (r < nRows && Data[r, 0] < key) { r++; }

            lastRowIndex = r;
            // make sure denominator below does not go to zero.

            Span = Data[r, 0] - Data[r - 1, 0];
            if (Span != 0.0)
            {
                Factor = (key - Data[r - 1, 0]) / Span;
                if (Factor > 1.0) Factor = 1.0;
            }
            else
            {
                Factor = 1.0;
            }

            Value = Factor * (Data[r, 1] - Data[r - 1, 1]) + Data[r - 1, 1];

            return Value;
        }
    }

    // taken from JSBSim
    [Serializable]
    public class AJEPropJSB : IConfigNode
    {
        string name;
        // FGThruster members
        double Thrust;
        double PowerRequired;
        public double deltaT;
        double GearRatio;
        double ThrustCoeff;
        //double ReverserAngle;

        //Vector3 ActingLocation;

        // FGPropeller members
        int numBlades;
        double J;
        double RPM;
        double Ixx;
        double Diameter;
        double MaxPitch;
        double MinPitch;
        double MinRPM;
        double MaxRPM;
        double Pitch;
        double P_Factor;
        double Sense;
        double Advance;
        double ExcessTorque;
        double D4;
        double D5;
        double HelicalTipMach;
        double Vinduced;
        Vector3d vTorque;
        FGTable cThrust;
        FGTable cPower;
        FloatCurve cPowerFP;
        FloatCurve cThrustFP;
        FloatCurve CtMach;
        FloatCurve CpMach;
        FloatCurve MachDrag;
        double CtFactor;
        double CpFactor;
        double CtTweak;
        double CpTweak;
        double MachPowTweak;
        int ConstantSpeed;
        double ReversePitch; // Pitch, when fully reversed
        bool Reversed;     // true, when propeller is reversed
        double Reverse_coef; // 0 - 1 defines AdvancePitch (0=MIN_PITCH 1=REVERSE_PITCH)
        bool Feathered;    // true, if feather command

        // constants
        const double HPTOFTLBSEC = 550;
        const double FTTOM = 0.3048;
        const double INTOM = 0.0254;
        const double LBTOKN = 0.0044482216;
        const double KGM3TOSLUGFT3 = 0.00194032033;
        const double MTOFT = 1 / FTTOM;
        const double FTLBTOJ = 1.3558179491; // W/HP divided by ft-lb/HP

        public void LogVars()
        {
            Debug.Log("Dumping engine status");
            foreach (FieldInfo f in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Debug.Log(f.Name + " = " + f.GetValue(this));
            }
            foreach (FieldInfo f in this.GetType().GetFields())
            {
                Debug.Log(f.Name + " = " + f.GetValue(this));
            }
        }
        

        public AJEPropJSB(ConfigNode node = null)
        {

            SetDefaults();
            RPM = 30;
            if (node != null)
                Load(node);
            CalcDefaults();
        }
        public AJEPropJSB(string propName, double minR = -1, double maxR = -1, double diam = -1, double ixx = -1)
        {
            SetDefaults();


            RPM = 30;
            ConfigNode node = null;
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PROPELLER"))
            {
                if (n.HasValue("name"))
                    if (n.GetValue("name").Equals(propName))
                    {
                        node = n;
                        break;
                    }
            }
            if (node != null)
                Load(node);
            if (minR >= 0)
                MinRPM = minR;
            if (maxR >= 0)
                MaxRPM = maxR;
            if (diam >= 0)
                Diameter = diam;
            if (ixx >= 0)
                Ixx = ixx;

            CalcDefaults();
            Debug.Log("*AJE* Constructed prop of type " + propName + ", RPM " + RPM + ", pitch " + MinPitch + "/" + MaxPitch + ", RPMs " + MinRPM + "/" + MaxRPM + ", Diam " + Diameter + "m, Ixx " + Ixx + "J. CS? " + ConstantSpeed);
        }
        public AJEPropJSB(AJEPropJSB t)
        {
            name = t.name;
            deltaT = t.deltaT;
            GearRatio = t.GearRatio;
            numBlades = t.numBlades;
            J = t.J;
            RPM = t.RPM;
            Ixx = t.Ixx;
            Diameter = t.Diameter;
            MaxPitch = t.MaxPitch;
            MinPitch = t.MinPitch;
            MinRPM = t.MinRPM;
            MaxRPM = t.MaxRPM;
            Pitch = t.Pitch;
            P_Factor =t.P_Factor;
            Sense = t.Sense;
            Advance = t.Advance;
            ExcessTorque = t.ExcessTorque;
            D4 = t.D4;
            D5 = t.D5;
            HelicalTipMach = t.HelicalTipMach;
            Vinduced = t.Vinduced;
            vTorque = t.vTorque;
            if (t.cThrust != null)
                cThrust = new FGTable(t.cThrust);
            else
                cThrustFP = t.cThrustFP;
            if (t.cPower != null)
                cPower = new FGTable(t.cPower);
            else
                cPowerFP = t.cPowerFP;

            CtMach = t.CtMach;
            CpMach = t.CpMach;
            MachDrag = t.MachDrag;
            CtFactor = t.CtFactor;
            CpFactor = t.CpFactor;
            ConstantSpeed = t.ConstantSpeed;
            ReversePitch = t.ReversePitch;
            Reversed = t.Reversed;
            Reverse_coef = t.Reverse_coef;
            Feathered = t.Feathered;
        }
        public void Load(ConfigNode node)
        {
            if (node.HasNode("PROPELLER"))
                node = node.GetNode("PROPELLER");

            if (node.HasValue("name"))
                name = node.GetValue("name");
            if (node.HasValue("ixx"))
                Ixx = double.Parse(node.GetValue("ixx"));
            if (node.HasValue("ixxFTLB"))
                Ixx = double.Parse(node.GetValue("ixxFTLB")) * FTLBTOJ;
            if (node.HasValue("diameterIN"))
                Diameter = double.Parse(node.GetValue("diameterIN")) * INTOM;
            if (node.HasValue("diameterFT"))
                Diameter = double.Parse(node.GetValue("diameterFT")) * FTTOM;
            if (node.HasValue("diameter"))
                Diameter = double.Parse(node.GetValue("diameter"));
            if (node.HasValue("numblades"))
                numBlades = int.Parse(node.GetValue("numblades"));
            if (node.HasValue("gearratio"))
                GearRatio = double.Parse(node.GetValue("gearratio"));
            if (node.HasValue("minpitch"))
                MinPitch = double.Parse(node.GetValue("minpitch"));
            if (node.HasValue("maxpitch"))
                MaxPitch = double.Parse(node.GetValue("maxpitch"));
            if (node.HasValue("minrpm"))
                MinRPM = double.Parse(node.GetValue("minrpm"));
            if (node.HasValue("maxrpm"))
            {
                MaxRPM = double.Parse(node.GetValue("maxrpm"));
                ConstantSpeed = 1;
            }
            if (node.HasValue("constspeed"))
                ConstantSpeed = int.Parse(node.GetValue("constspeed"));
            if (node.HasValue("reversepitch"))
                ReversePitch = double.Parse(node.GetValue("reversepitch"));

            if (node.HasNode("cThrust"))
                cThrust = new FGTable(node.GetNode("cThrust"));
            if (node.HasNode("cThrustFP"))
            {
                cThrustFP = new FloatCurve();
                cThrustFP.Load(node.GetNode("cThrustFP"));
            }
            if (node.HasNode("cPower"))
                cPower = new FGTable(node.GetNode("cPower"));
            if (node.HasNode("cPowerFP"))
            {
                cPowerFP = new FloatCurve();
                cPowerFP.Load(node.GetNode("cPowerFP"));
            }
            if (node.HasNode("CtMach"))
            {
                CtMach = new FloatCurve();
                CtMach.Load(node.GetNode("CtMach"));
            }
            if (node.HasNode("CpMach"))
            {
                CpMach = new FloatCurve();
                CpMach.Load(node.GetNode("CpMach"));
            }
            if (node.HasNode("MachDrag"))
            {
                MachDrag = new FloatCurve();
                MachDrag.Load(node.GetNode("MachDrag"));
            }

            if (node.HasValue("sense"))
                Sense = double.Parse(node.GetValue("sense"));
            SetSense(Sense >= 0.0 ? 1.0 : -1.0);
            if (node.HasValue("P_Factor"))
                P_Factor = double.Parse(node.GetValue("P_Factor"));
            if (node.HasValue("ct_factor"))
                SetCtFactor(double.Parse(node.GetValue("ct_factor")));
            if (node.HasValue("cp_factor"))
                SetCpFactor(double.Parse(node.GetValue("cp_factor")));

            CalcDefaults();
            // persistent values
            if (node.HasValue("RPM"))
                RPM = double.Parse(node.GetValue("RPM"));
            if (node.HasValue("Pitch"))
                Pitch = double.Parse(node.GetValue("Pitch"));
            if (node.HasValue("Feathered"))
                Feathered = bool.Parse(node.GetValue("Feathered"));
            if (node.HasValue("Reversed"))
                Reversed = bool.Parse(node.GetValue("Reversed"));
        }
        public void Save(ConfigNode node)
        {
            node.AddValue("RPM", RPM);
            node.AddValue("Pitch", Pitch);
            node.AddValue("Reversed", Reversed);
            node.AddValue("Feathered", Feathered);
        }

        void SetDefaults()
        {
            MaxPitch = MinPitch = P_Factor = Pitch = Advance = MinRPM = MaxRPM = deltaT = 0.0;
            Sense = 1; // default clockwise rotation
            ReversePitch = 0.0;
            Reversed = false;
            Feathered = false;
            Reverse_coef = 0.0;
            GearRatio = 1.0;
            CtFactor = CpFactor = CpTweak = CtTweak = MachPowTweak = 1.0;
            ConstantSpeed = 1; // assume constant speed unless told otherwise
            cThrust = null;
            cThrustFP = null;
            cPower = null;
            cPowerFP = null;
            CtMach = null;
            CpMach = null;
            MachDrag = null;
            Vinduced = 0.0;
        }
        void CalcDefaults()
        {
            if (MinPitch == MaxPitch)
                ConstantSpeed = 0;
            else
                ConstantSpeed = 1;
            vTorque = new Vector3(0f, 0f, 0f);
            D4 = Diameter * Diameter * Diameter * Diameter;
            D5 = D4 * Diameter;
            Pitch = MinPitch;
        }

        /** Checks if the object is sane (because KSP serialization is poor) */
        public bool IsSane()
        {
            if (MinPitch == MaxPitch && MaxPitch == 0 && ConstantSpeed == 1)
                return false;
            return true;
        }

        // Set tweak values
        public void SetTweaks(double newCtTweak, double newCpTweak, double newMachTweak)
        {
            CtTweak = newCtTweak;
            CpTweak = newCpTweak;
            MachPowTweak = newMachTweak;
        }

        /** Sets the Revolutions Per Minute for the propeller. Normally the propeller
            instance will calculate its own rotational velocity, given the Torque
            produced by the engine and integrating over time using the standard
            equation for rotational acceleration "a": a = Q/I , where Q is Torque and
            I is moment of inertia for the propeller.
            @param rpm the rotational velocity of the propeller */
        public void SetRPM(double rpm) { RPM = rpm; }

        /*/// Sets the Revolutions Per Minute for the propeller using the engine gear ratio
        public void SetEngineRPM(double rpm) { RPM = rpm / GearRatio; }*/

        /// Returns true of this propeller is variable pitch
        public bool IsVPitch() { return MaxPitch != MinPitch; }

        /** This commands the pitch of the blade to change to the value supplied.
            This call is meant to be issued either from the cockpit or by the flight
            control system (perhaps to maintain constant RPM for a constant-speed
            propeller). This value will be limited to be within whatever is specified
            in the config file for Max and Min pitch. It is also one of the lookup
            indices to the power and thrust tables for variable-pitch propellers.
            @param pitch the pitch of the blade in degrees. */
        public void SetPitch(double pitch) { Pitch = pitch; }

        public void SetAdvance(double advance) { Advance = advance; }

        /// Sets the P-Factor constant
        public void SetPFactor(double pf) { P_Factor = pf; }

        /// Sets propeller into constant speed mode, or manual pitch mode
        public void SetConstantSpeed(int mode) { ConstantSpeed = mode; }

        /// Sets coefficient of thrust multiplier
        public void SetCtFactor(double ctf) { CtFactor = ctf; }

        /// Sets coefficient of power multiplier
        public void SetCpFactor(double cpf) { CpFactor = cpf; }

        /** Sets the rotation sense of the propeller.
            @param s this value should be +/- 1 ONLY. +1 indicates clockwise rotation as
                     viewed by someone standing behind the engine looking forward into
                     the direction of flight. */
        public void SetSense(double s) { Sense = s; }

        /// Retrieves the pitch of the propeller in degrees.
        public double GetPitch() { return Pitch; }

        /// Retrieves the RPMs of the propeller
        public double GetRPM() { return RPM; }

        /*/// Calculates the RPMs of the engine based on gear ratio
        public double GetEngineRPM() { return RPM * GearRatio; }*/

        /// Retrieves the propeller moment of inertia
        public double GetIxx() { return Ixx; }

        /// Retrieves the coefficient of thrust multiplier
        public double GetCtFactor() { return CtFactor; }

        /// Retrieves the coefficient of power multiplier
        public double GetCpFactor() { return CpFactor; }

        /// Retrieves the propeller diameter
        public double GetDiameter() { return Diameter; }

        /// Retrieves propeller thrust table
        public FGTable GetCThrustTable() { return cThrust; }
        public FloatCurve GetCThrustFPTable() { return cThrustFP; }
        /// Retrieves propeller power table
        public FGTable GetCPowerTable() { return cPower; }
        public FloatCurve GetCPowerFPTable() { return cPowerFP; }
        /// Retrieves propeller thrust Mach effects factor
        public FloatCurve GetCtMachTable() { return CtMach; }
        /// Retrieves propeller power Mach effects factor
        public FloatCurve GetCpMachTable() { return CpMach; }

        public FloatCurve GetMachDragTable() { return MachDrag; }

        /// Retrieves the Torque in foot-pounds (Don't you love the English system?)
        public double GetTorque() { return vTorque.x; }

        public void SetReverseCoef(double c) { Reverse_coef = c; }
        public double GetReverseCoef() { return Reverse_coef; }
        public void SetReverse(bool r) { Reversed = r; }
        public bool GetReverse() { return Reversed; }
        public void SetFeather(bool f) { Feathered = f; }
        public bool GetFeather() { return Feathered; }
        public double GetThrustCoefficient() { return ThrustCoeff; }
        public double GetHelicalTipMach() { return HelicalTipMach; }
        public int GetConstantSpeed() { return ConstantSpeed; }
        public void SetInducedVelocity(double Vi) { Vinduced = Vi; }
        public double GetInducedVelocity() { return Vinduced; }

        /*public Vector3 GetPFactor()
        {
            double px = 0.0, py, pz;

            py = Thrust * Sense * (ActingLocation.y - GetLocationY()) / 12.0;
            pz = Thrust * Sense * (ActingLocation.z - GetLocationZ()) / 12.0;

            return Vector3(px, py, pz);
        }*/

        /** Retrieves the power required (or "absorbed") by the propeller -
            i.e. the power required to keep spinning the propeller at the current
            velocity, air density,  and rotational rate. */
        public double GetPowerRequired(double rho, double Vel)
        {
            double cPReq, J;
            double RPS = RPM / 60.0;

            if (RPS != 0.0)
                J = Vel / (Diameter * RPS);
            else
                J = Vel / Diameter;

            if (MaxPitch == MinPitch)   // Fixed pitch prop
            {
                cPReq = cPowerFP.Evaluate((float)J);
            }
            else
            {                      // Variable pitch prop
                if (ConstantSpeed != 0)   // Constant Speed Mode
                {

                    // do normal calculation when propeller is neither feathered nor reversed
                    // Note:  This method of feathering and reversing was added to support the
                    //        turboprop model.  It's left here for backward compatablity, but
                    //        now feathering and reversing should be done in Manual Pitch Mode.
                    if (!Feathered)
                    {
                        if (!Reversed)
                        {
                            double rpmReq = MinRPM + (MaxRPM - MinRPM) * Advance;
                            double dRPM = rpmReq - RPM;
                            // The pitch of a variable propeller cannot be changed when the RPMs are
                            // too low - the oil pump does not work.
                            if (RPM > 200) Pitch -= dRPM * deltaT;
                            if (Pitch < MinPitch) Pitch = MinPitch;
                            else if (Pitch > MaxPitch) Pitch = MaxPitch;

                        }
                        else // Reversed propeller
                        {
                            // when reversed calculate propeller pitch depending on throttle lever position
                            // (beta range for taxing full reverse for braking)
                            double PitchReq = MinPitch - (MinPitch - ReversePitch) * Reverse_coef;
                            // The pitch of a variable propeller cannot be changed when the RPMs are
                            // too low - the oil pump does not work.
                            if (RPM > 200) Pitch += (PitchReq - Pitch) / 200;
                            if (RPM > MaxRPM)
                            {
                                Pitch += (MaxRPM - RPM) / 50;
                                if (Pitch < ReversePitch) Pitch = ReversePitch;
                                else if (Pitch > MaxPitch) Pitch = MaxPitch;
                            }
                        }

                    }
                    else  // Feathered propeller
                    {
                        // ToDo: Make feathered and reverse settings done via FGKinemat
                        Pitch += (MaxPitch - Pitch) / 300; // just a guess (about 5 sec to fully feathered)
                        if (Pitch > MaxPitch)
                            Pitch = MaxPitch;
                    }
                }
                else // Manual Pitch Mode, pitch is controlled externally
                {
                }
                cPReq = cPower.GetValue(J, Pitch);
            }

            // Apply optional scaling factor to Cp (default value = 1)
            cPReq *= CpFactor * CpTweak;

            // Apply optional Mach effects from CP_MACH table
            if (CpMach != null)
                cPReq *= Math.Pow(CpMach.Evaluate((float)HelicalTipMach), MachPowTweak);

            double local_RPS = RPS < 0.01 ? 0.01 : RPS;

            PowerRequired = cPReq * local_RPS * RPS * local_RPS * D5 * rho;
            vTorque.x = (-Sense * PowerRequired / (local_RPS * 2.0 * Math.PI));

            return PowerRequired;
        }

        /** Calculates and returns the thrust produced by this propeller.
            Given the excess power available from the engine (in foot-pounds), the thrust is
            calculated, as well as the current RPM. The RPM is calculated by integrating
            the torque provided by the engine over what the propeller "absorbs"
            (essentially the "drag" of the propeller).
            @param PowerAvailable this is the excess power provided by the engine to
            accelerate the prop. It could be negative, dictating that the propeller
            would be slowed.
            @return the thrust in pounds */
        public double Calculate(double EnginePower, double rho, double Vel, double speedOfSound)
        {
            EnginePower *= PistonEngine.HP2W;
            /*rho *= KGM3TOSLUGFT3;
            Vel *= MSECTOFTSEC;
            speedOfSound *= MSECTOFTSEC;*/

            double omega, PowerAvailable;
            double RPS = RPM/60.0;
            double machInv = 1 / speedOfSound;
            // Calculate helical tip Mach
            double Area = 0.25*Diameter*Diameter*Math.PI;
            double Vtip = RPS * Diameter * Math.PI;
            HelicalTipMach = Math.Sqrt(Vtip * Vtip + Vel * Vel) * machInv;

            PowerAvailable = EnginePower - GetPowerRequired(rho, Vel);

            if (RPS > 0.0)
                J = Vel / (Diameter * RPS); // Calculate J normally
            else
                J = Vel / Diameter;

            if (MaxPitch == MinPitch)     // Fixed pitch prop
                ThrustCoeff = cThrustFP.Evaluate((float)J);
            else                       // Variable pitch prop
                ThrustCoeff = cThrust.GetValue(J, Pitch);

            // Apply optional scaling factor to Ct (default value = 1)
            ThrustCoeff *= CtFactor * CtTweak;

            // Apply optional Mach effects from CT_MACH table
            if (CtMach != null)
                ThrustCoeff *= Math.Pow(CtMach.Evaluate((float)HelicalTipMach), MachPowTweak);

            Thrust = ThrustCoeff*RPS*RPS*D4*rho;

            /*// Induced velocity in the propeller disk area. This formula is obtained
            // from momentum theory - see B. W. McCormick, "Aerodynamics, Aeronautics,
            // and Flight Mechanics" 1st edition, eqn. 6.15 (propeller analysis chapter).
            // Since Thrust and Vel can both be negative we need to adjust this formula
            // To handle sign (direction) separately from magnitude.
            double Vel2sum = Vel*Math.Abs(Vel) + 2.0*Thrust/(rho*Area);
  
            if( Vel2sum > 0.0)
                Vinduced = 0.5 * (-Vel + Math.Sqrt(Vel2sum));
            else
                Vinduced = 0.5 * (-Vel - Math.Sqrt(-Vel2sum));

            // We need to drop the case where the downstream velocity is opposite in
            // direction to the aircraft velocity. For example, in such a case, the
            // direction of the airflow on the tail would be opposite to the airflow on
            // the wing tips. When such complicated airflows occur, the momentum theory
            // breaks down and the formulas above are no longer applicable
            // (see H. Glauert, "The Elements of Airfoil and Airscrew Theory",
            // 2nd edition, ?6.3, pp. 219-221)

            if ((Vel+2.0*Vinduced)*Vel < 0.0)
            // The momentum theory is no longer applicable so let's assume the induced
            // saturates to -0.5*Vel so that the total velocity Vel+2*Vinduced equals 0.
                Vinduced = -0.5*Vel;
    
            // P-factor is simulated by a shift of the acting location of the thrust.
            // The shift is a multiple of the angle between the propeller shaft axis
            // and the relative wind that goes through the propeller disk.
            if (P_Factor > 0.0001) {
            double tangentialVel = localAeroVel.Magnitude(eV, eW);

            if (tangentialVel > 0.0001) {
                double angle = atan2(tangentialVel, localAeroVel(eU));
                double factor = Sense * P_Factor * angle / tangentialVel;
                SetActingLocationY( GetLocationY() + factor * localAeroVel(eW));
                SetActingLocationZ( GetLocationZ() + factor * localAeroVel(eV));
            }
            }*/

            omega = RPS*2.0*Math.PI;

            // The Ixx value and rotation speed given below are for rotation about the
            // natural axis of the engine. The transform takes place in the base class
            // FGForce::GetBodyForces() function.

            /*vH(eX) = Ixx*omega*Sense;
            vH(eY) = 0.0;
            vH(eZ) = 0.0;*/

            if (omega > 0.0)
                ExcessTorque = PowerAvailable / omega;
            else
                ExcessTorque = PowerAvailable / 1.0;

            RPM = (RPS + ((ExcessTorque / Ixx) / (2.0 * Math.PI)) * deltaT) * 60.0;

            if (RPM < 0.0) RPM = 0.0; // Engine won't turn backwards

            // Transform Torque and momentum first, as PQR is used in this
            // equation and cannot be transformed itself.
            //vMn = in.PQR*(Transform()*vH) + Transform()*vTorque;
            if(MachDrag != null)
            {
                double machDrag = MachDrag.Evaluate((float)(Vel * machInv));
                machDrag *= machDrag * D4 * RPS * RPS * rho * 0.00004;
                Thrust -= machDrag;
            }
            return Thrust * 0.001; // return thrust in kilonewtons
        }
    }
}