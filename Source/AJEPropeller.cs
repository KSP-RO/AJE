using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{

    public class AJEPropeller : PartModule
    {


        [KSPField(isPersistant = false, guiActive = false)]
        public float IspMultiplier = 1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool useOxygen = true;
        [KSPField(isPersistant = false, guiActive = false)]
        public float idle = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float r0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "v0", guiFormat = "000"), UI_FloatRange(minValue = 300f, maxValue = 1600f, stepIncrement = 5f)]
        public float v0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float omega0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "rho0", guiFormat = "0.###"), UI_FloatRange(minValue = 0.26f, maxValue = 1.26f, stepIncrement = 0.005f)]
        public float rho0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "power0", guiFormat = "000"), UI_FloatRange(minValue = 300.00f, maxValue = 4000.0f, stepIncrement = 10f)]
        public float power0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float fine;
        [KSPField(isPersistant = false, guiActive = false)]
        public float coarse;
        [KSPField(isPersistant = false, guiActive = false)]
        public float omega;
        [KSPField(isPersistant = false, guiActive = false)]
        public float power;
        [KSPField(isPersistant = false, guiActive = false)]
        public float gearratio;
        [KSPField(isPersistant = false, guiActive = false)]
        public float turbo = 1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float BSFC = 7.62e-08f;
        [KSPField(isPersistant = false, guiActive = true)]
        public string ShaftPower;
        [KSPField(isPersistant = false, guiActive = false)]
        public float SpeedBuff = 1.0f;
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
        public float coolerEffic = 0f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float coolerMin = -200f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float ramAir = 0.2f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Manifold Pressure (inHG)")]
        public float manifoldPressure = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Flow (kg/s)")]
        public float fuelFlow = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge Air Temp")]
        public float chargeAirTemp = 15.0f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Exhaust Thrust (kN)")]
        public float netExhaustThrust = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Boost", guiFormat = "0.##"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float boost = 1.0f;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Mixture", guiFormat = "0.###"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.005f)]
        public float mixture = 0.836481f; // optimal "auto rich"
        // ignore RPM for now

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "VolETweak", guiFormat = "0.##"), UI_FloatRange(minValue = 0.25f, maxValue = 1.5f, stepIncrement = 0.01f)]
        public float VolETweak = 1.0f;

//        [KSPField(isPersistant = false, guiActive = true)]
        public float density = 1.225f;
//        [KSPField(isPersistant = false, guiActive = true)]
        public float pressure = 101325f;
//        [KSPField(isPersistant = false, guiActive = true)]
        public float temperature = 15f;
//        [KSPField(isPersistant = false, guiActive = true)]
        public float v;
//        [KSPField(isPersistant = false, guiActive = true)]
        public float targetTorque;
 //       [KSPField(isPersistant = false, guiActive = true)]
        public float propTorque;
//        [KSPField(isPersistant = false, guiActive = true)]
        public float thrust;
 //       [KSPField(isPersistant = false, guiActive = true)]
        public float isp;

        public const float INHG2PA = 101325.0f / 760f * 1000f * 0.0254f; // 1 inch of Mercury in Pascals


 //       [KSPField(isPersistant = false, guiActive = true)]
 //       public string thrustvector;
 //       [KSPField(isPersistant = false, guiActive = true)]
//          public string velocityvector;

        public AJEPropellerSolver propeller;
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
   
    //        v0 *= 0.5144f;
    //        omega0 *= 0.1047f;
    //        power0 *= 745.7f;
     //       omega *= 0.1047f;
     //       power *= 745.7f;

            propeller = new AJEPropellerSolver(r0, v0 * 0.5144f, omega0 * PistonEngine.RPM2RADPS, rho0, power0 * PistonEngine.HP2W);
            pistonengine = new PistonEngine(power * PistonEngine.HP2W, omega * PistonEngine.RPM2RADPS / gearratio, BSFC);
            pistonengine._hasSuper = true;
            if (!pistonengine.setBoostParams(wastegateMP * INHG2PA, boost0 * INHG2PA, boost1 * INHG2PA, rated0, rated1, cost1 * PistonEngine.HP2W, switchAlt))
                pistonengine.setTurboParams(turbo, wastegateMP * INHG2PA);
            if (displacement > 0)
                pistonengine._displacement = displacement * PistonEngine.CIN2CM;
            pistonengine._compression = compression;
            pistonengine._coolerEffic = coolerEffic;
            pistonengine._coolerMin = coolerMin + 273.15f;
            pistonengine._ramAir = ramAir;

            pistonengine.ComputeVEMultiplier(); // given newly-set stats

            propeller.setStops(fine, coarse);
            
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

            density = (float)ferram4.FARAeroUtil.GetCurrentDensity(part.vessel.mainBody, (float)part.vessel.altitude);
            v = Vector3.Dot(vessel.srf_velocity,-part.FindModelTransform(engine.thrustVectorTransformName).forward.normalized);
            pressure = (float)FlightGlobals.getStaticPressure(vessel.altitude, vessel.mainBody) * 101325f + 0.5f * density * v * v * ramAir; // include dynamic pressure
            temperature = FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.15f;

            propeller.calc(density, v / SpeedBuff, omega * PistonEngine.RPM2RADPS);



            pistonengine.calc(pressure, temperature, omega * PistonEngine.RPM2RADPS / gearratio);
            
            if (!useOxygen)
            {
                pistonengine._power = power * PistonEngine.HP2W;
                pistonengine._torque = power * PistonEngine.HP2W / (omega * PistonEngine.RPM2RADPS);
            }

            ShaftPower = ((int)Math.Round(pistonengine._power/PistonEngine.HP2W)).ToString()+"HP";
            manifoldPressure = pistonengine._mp / INHG2PA;
            fuelFlow = pistonengine._fuelFlow;
            chargeAirTemp = pistonengine._chargeTemp - 273.15f;

            float mod = 1;
            targetTorque = pistonengine._torque/gearratio;
            propTorque = propeller._torqueOut;
            float momt = 500f;

            mod = propTorque < targetTorque ? 1.04f : (1.0f / 1.04f);
            float diff = Mathf.Abs((propTorque - targetTorque) / momt);
            if (diff < 10)
                mod = 1 + (mod - 1) * (0.1f * diff);
            
            propeller.modPitch(mod);

            thrust = propeller._thrustOut / 1000f;
            // exhaust thrust, normalized for 2200HP at 7km = 200lbs thrust
            netExhaustThrust = exhaustThrust * (float)(pistonengine._power / (1640540 / 0.89) * Math.Min(.05+(1-vessel.staticPressure)*1.6,1.0));
            thrust += netExhaustThrust;
            engine.SetThrust(thrust);
            isp = propeller._thrustOut / 9.80665f / BSFC / pistonengine._power;
            engine.SetIsp(isp);
            
//            Vector3d v1 = part.FindModelTransform("thrustTransform").forward;
//            v1 = vessel.ReferenceTransform.InverseTransformDirection(v1)*100;
 //           Vector3d v2 = vessel.srf_velocity;
 //           v2 = vessel.ReferenceTransform.InverseTransformDirection(v2);
  //          thrustvector = ((int)v1.x).ToString() + " " + ((int)v1.y).ToString() + " " + ((int)v1.z).ToString() + " " + ((int)v1.magnitude).ToString();
 //           velocityvector = ((int)v2.x).ToString() + " " + ((int)v2.y).ToString() + " " + ((int)v2.z).ToString() + " " + ((int)v2.magnitude).ToString();
            
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

        public const float HP2W = 745.699872f;
        public const float CIN2CM = 1.6387064e-5f;
        public const float RPM2RADPS = 0.1047198f;
        public const float RAIR = 287.3f;
        public FloatCurve mixtureEfficiency; //fuel:air -> efficiency

        const float T0 = 288.15f;
        const float P0 = 101325f;

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
            node.AddValue("key", "0.05	0.0");
            node.AddValue("key", "0.05137	0.00862");
            node.AddValue("key", "0.05179	0.21552");
            node.AddValue("key", "0.0543	0.48276");
            node.AddValue("key", "0.05842	0.7069");
            node.AddValue("key", "0.06312	0.83621");
            node.AddValue("key", "0.06942	0.93103");
            node.AddValue("key", "0.07786	1.0");
            node.AddValue("key", "0.08845	1.0");
            node.AddValue("key", "0.0927	0.98276");
            node.AddValue("key", "0.1012	0.93103");
            node.AddValue("key", "0.11455	0.72414");
            node.AddValue("key", "0.12158	0.4569");
            node.AddValue("key", "0.12435	0.23276");
            node.AddValue("key", "0.125 0.0");
            mixtureEfficiency.Load(node);


            ComputeVEMultiplier(); // compute volumetric efficiency of engine, based on rated power and BSFC.
        }

        // return the fuel:air ratio of the given mixture setting.
        // mix = range [0, 1]
        public float FuelAirRatio(float mix) 
        {
            return 1f / (8.05f + 11.2f * (1f - mix)); // prevent an AFR too high or low
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
            float fuelAirRatio = FuelAirRatio(0.836481f);
            float MAP = Math.Min(_maxMP, P0 * _boostMults[0]);
            float m_dot_air = GetAirflow(P0, T0, _omega0, MAP);
            float m_dot_fuel = fuelAirRatio * m_dot_air;
            float power = m_dot_fuel * mixtureEfficiency.Evaluate(fuelAirRatio) / _bsfc;
            _voleffic = _power0 / power;
            float m_dot_air2 = GetAirflow(P0, T0, _omega0, MAP);
            float m_dot_fuel2 = fuelAirRatio * m_dot_air;
            power = m_dot_fuel2 * mixtureEfficiency.Evaluate(fuelAirRatio) / _bsfc;
            MonoBehaviour.print("*AJE* Setting volumetric efficiency. At SL with MAP " + MAP + ", power = " + power / HP2W + "HP, BSFC = " + _bsfc + ", mda/f = " + m_dot_air2 + "/" + m_dot_fuel2 + ", VE = " + _voleffic + ". Orig a/f: " + m_dot_air + "/" + m_dot_fuel);
        }
        
        // legacy support
        public void setTurboParams(float turbo, float maxMP)
        {
            _maxMP = maxMP;
            _boostMults[0] = turbo;
            _boostCosts[0] = 0f;
        }

        // set boost parameters:
        // maximum MAP, the two boost pressures to maintain, the two rated altitudes (km),
        // the cost for the second boost mode, and the switch altitude (in km), or -1f for auto
        public bool setBoostParams(float wastegate, float boost0, float boost1, float rated0, float rated1, float cost1, float switchAlt)
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

            // Scale to throttle setting, clamp to wastegate
            if (_running)
                MAP *= _minMP + (1 - _minMP) * _throttle;

            // Scale the max MP according to the WASTEGATE control input.  Use
            // the un-supercharged MP as the bottom limit.
            return (float)Math.Min(MAP, Math.Max(_wastegate * _maxMP, pAmb));
        }

        // Iteration method
        // Will calculate engine parameters.
        // Takes ambient pressure, temperature, and the engine revolutions in radians/sec
        public void calc(float pAmb, float tAmb, float speed)
        {
            _running = true; //_magnetos && _fuel && (speed > 60*RPM2RADPS);

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
                if (_hasSuper)
                {
                    // Superchargers have no lag
                    _charge = _chargeTarget;
                } //else if(!_running) {
                // Turbochargers only work well when the engine is actually
                // running.  The 25% number is a guesstimate from Vivian.
                //   _chargeTarget = 1 + (_chargeTarget - 1) * 0.25;
                //  }

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
                float MAP0 = CalcMAP(pAmb, 0, target0);
                float MAP1 = CalcMAP(pAmb, 1, target1);

                float m_dot_fuel0 = GetAirflow(pAmb, tAmb, speed, MAP0) * fuelRatio;
                float power0 = m_dot_fuel0 * mixtureEfficiency.Evaluate(fuelRatio) / _bsfc - _boostCosts[0];

                float m_dot_fuel1 = GetAirflow(pAmb, tAmb, speed, MAP1) * fuelRatio;
                float power1 = m_dot_fuel1 * mixtureEfficiency.Evaluate(fuelRatio) / _bsfc - _boostCosts[1];

                if (power0 >= power1)
                {
                    MAP = MAP0;
                    _chargeTarget = _charge = target0;
                    power = power0;
                    _fuelFlow = m_dot_fuel0;
                }
                else
                {
                    MAP = MAP1;
                    _chargeTarget = _charge = target1;
                    power = power1;
                    _fuelFlow = m_dot_fuel1;
                }
            }

            _mp = MAP;
            _chargeTemp = GetCAT(MAP, pAmb, tAmb); // duplication of effort, but oh well

            // The "boost" is the delta above ambient
            _boostPressure = _mp - pAmb;

            _power = power;
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


    public class AJEPropellerSolver
    {

        public float _r;           // characteristic radius
        public float _j0;          // zero-thrust advance ratio
        public float _baseJ0;      //  ... uncorrected for prop advance
        public float _f0;          // thrust coefficient
        public float _etaC;        // Peak efficiency
        public float _lambdaPeak;  // constant, ~0.759835;
        public float _beta;        // constant, ~1.48058;
        public float _tc0;         // thrust "coefficient" at takeoff
        public float _fine_stop;   // ratio for minimum pitch (high RPM)
        public float _coarse_stop; // ratio for maximum pitch (low RPM)
        public bool _matchTakeoff; // Does _tc0 mean anything?
        public bool _manual;      // manual pitch mode
        public float _proppitch;   // prop pitch control setting (0 ~ 1.0)
        public float _propfeather; // prop feather control setting (0 = norm, 1 = feather)

        public float _density;

        public float _thrustOut;
        public float _torqueOut;
        public bool _autopitch;
        public float _v0;


        public AJEPropellerSolver(float radius, float v, float omega, float rho, float power)
        {
            // Initialize numeric constants:
            _lambdaPeak = Mathf.Pow(1f / 9f, 1f / 8f);
            _beta = 1.0f / (_lambdaPeak - Mathf.Pow(_lambdaPeak, 9f));

            _r = radius;
            _etaC = 0.85f; // make this settable?
            _v0 = v;
            _j0 = v / (omega * _lambdaPeak);
            _baseJ0 = _j0;

            float V2 = v * v + (_r * omega) * (_r * omega);
            _f0 = 2 * _etaC * power / (rho * v * V2);

            _matchTakeoff = false;
            _manual = false;
            _proppitch = 0;
        }

        public void setTakeoff(float omega0, float power0)
        {
            // Takeoff thrust coefficient at lambda==0
            _matchTakeoff = true;
            float V2 = _r * omega0 * _r * omega0;
            float gamma = _etaC * _beta / _j0;
            float torque = power0 / omega0;

            _tc0 = (torque * gamma) / (0.5f * _density * V2 * _f0);
        }

        public void setStops(float fine_stop, float coarse_stop)
        {
            _fine_stop = fine_stop;
            _coarse_stop = coarse_stop;
        }

        public void modPitch(float mod)
        {
            _j0 *= mod;
            if (_j0 < _fine_stop * _baseJ0) _j0 = _fine_stop * _baseJ0;
            if (_j0 > _coarse_stop * _baseJ0) _j0 = _coarse_stop * _baseJ0;
        }

        public void setManualPitch()
        {
            _manual = true;
        }

        public void setPropPitch(float proppitch)
        {
            // makes only positive range of axis effective.
            if (proppitch < 0)
            {
                _proppitch = 0;
                return;
            }
            if (proppitch > 1)
            {
                _proppitch = 1;
                return;
            }
            _proppitch = proppitch;

        }

        public void setPropFeather(int state)
        {
            // 0 = normal, 1 = feathered
            if (state == 0)
            {
                _propfeather = 0;
            }
            else
            {
                _propfeather = 1;
            }
        }

        public void calc(float density, float v, float omega)
        {
            // For manual pitch, exponentially modulate the J0 value between
            // 0.25 and 4.  A prop pitch of 0.5 results in no change from the
            // base value.
            // TODO: integrate with _fine_stop and _coarse_stop variables
            if (_manual)
                _j0 = _baseJ0 * Mathf.Pow(2f, 2 - 4 * _proppitch);

            float tipspd = _r * omega;
            float V2 = v * v + tipspd * tipspd;

            // Sanify
            if (v < 0) v = 0;
            if (omega < 0.001f) omega = 0.001f;

            float J = v / omega;    // Advance ratio


            float lambda = J / _j0; // Unitless scalar advance ratio

            // There's an undefined point at lambda == 1.
            if (lambda == 1.0f) lambda = 0.9999f;

            float l4 = lambda * lambda; l4 = l4 * l4;   // lambda^4
            float gamma = (_etaC * _beta / _j0) * (1 - l4); // thrust/torque ratio

            // Compute a thrust coefficient, with clamping at very low
            // lambdas (fast propeller / slow aircraft).
            float tc = (1 - lambda) / (1 - _lambdaPeak);
            if (_matchTakeoff && tc > _tc0) tc = _tc0;

            float thrust = 0.5f * density * V2 * _f0 * tc;
            float torque = thrust / gamma;
            if (lambda > 1)
            {
                // This is the negative thrust / windmilling regime.  Throw
                // out the efficiency graph approach and instead simply
                // extrapolate the existing linear thrust coefficient and a
                // torque coefficient that crosses the axis at a preset
                // windmilling speed.  The tau0 value is an analytically
                // calculated (i.e. don't mess with it) value for a torque
                // coefficient at lamda==1.
                float tau0 = (0.25f * _j0) / (_etaC * _beta * (1 - _lambdaPeak));
                float lambdaWM = 1.2f; // lambda of zero torque (windmilling)
                torque = tau0 - tau0 * (lambda - 1) / (lambdaWM - 1);
                torque *= 0.5f * density * V2 * _f0;
            }

            _thrustOut = thrust;
            _torqueOut = torque;
        }

    }

}