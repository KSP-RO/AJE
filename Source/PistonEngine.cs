using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using System.Reflection;
using SolverEngines;

namespace AJE
{
    public class PistonEngine : ITorqueProducer
    {
        #region Members
        public double _throttle = 1d;
        public bool _starter = false; // true=engaged, false=disengaged
        public int _magnetos = 0; // 0=off, 1=right, 2=left, 3=both
        public float _mixture = 0.7f; //optimal
        public bool _fuel;
        public bool _running = false;
        public double _power0;   // reference power setting
        public double _rpm0;   //   "       engine speed
        public bool _hasSuper;  // true indicates gear-driven (not turbo)
        public double _turboLag; // turbo lag time in seconds
        public double _charge;   // current {turbo|super}charge multiplier
        public double _chargeTarget;  // eventual charge value
        public double _maxMP;    // static maximum pressure
        public double _wastegate;    // wastegate setting, [0:1]
        public double _displacement; // piston stroke volume
        public double _compression;  // compression ratio (>1)
        public double _minthrottle; // minimum throttle [0:1]
        public double _voleffic; // volumetric efficiency
        public double _volEfficMult; // multiplier to VE, for tweaking.
        public double[] _boostMults;
        public double[] _boostCosts;
        public int _boostMode;
        public double _boostSwitch;
        public double _bsfc;
        public double _bsfcRecip;
        public double _coolerEffic;
        public double _coolerMin;
        public double _ramAir;
        public double _exhaustThrust = 0d;
        public double _meredithEffect = 0d;

        public string _status = "Windmilling";

        // Runtime state/output:
        public double _mp;
        public double _fuelFlow;
        public double _airFlow;
        public double _egt;
        public double _boostPressure;
        public double _oilTemp;
        public double _oilTempTarget;
        public double _dOilTempdt;
        public double _power;
        public double _chargeTemp;
        public double _chargeDensity;
        public double _totalPower;
        public double _engineThrust;
        public double _netMeredithEffect;
        public double _netExhaustThrust;

        public const double HP2W = 745.699872d;
        public const double W2HP = 1d / HP2W;
        public const double PS2W = 735.49875d;
        public const double W2PS = 1d / PS2W;
        public const double LBFTON = 4.44822162d; // 1 pound-force in newtons
        public const double CIN2CM = 1.6387064e-5d;
        public const double RPM2RADPS = 1d / 60d * (2d * Math.PI);
        public const double RADPS2RPM = 1d / RPM2RADPS;
        public FloatCurve mixtureEfficiency; //fuel:air -> efficiency

        const double T0 = 288.15d; // 15C, reference temp at sea level
        const double P0 = 101325d;
        const double RAIR0 = 287d;
        const double GAMMA0 = 1.4d;
        #endregion

        #region Setup
        public PistonEngine(double power, double speed, double BSFC, double ramair, double displacement,
            double compression, double coolerEffic, double coolerMin, double exhaustThrust, double meredithEffect,
            double wastegate, double boost0, double boost1, double rated0, double rated1, double cost1, double switchAlt, bool turbo)
        {
            _running = false;
            _fuel = true;
            _boostPressure = 0;
            _hasSuper = false;
            _boostMults = new double[2];
            _boostCosts = new double[2];
            _boostMults[0] = 1d;
            _boostMults[1] = 1d;
            _boostCosts[0] = 0d;
            _boostCosts[1] = 0d;
            _boostMode = 0;
            _boostSwitch = -1; // auto
            _coolerEffic = coolerEffic;
            _coolerMin = coolerMin;
            _ramAir = ramair;
            _exhaustThrust = exhaustThrust;
            _meredithEffect = meredithEffect;
            _volEfficMult = 1.0d;

            _oilTemp = 298d;
            _oilTempTarget = _oilTemp;
            _dOilTempdt = 0;

            _bsfc = BSFC;
            _bsfcRecip = 1d / _bsfc;

            _minthrottle = 0.1d;
            _maxMP = 1e6d; // No waste gate on non-turbo engines.
            _wastegate = 1d;
            _charge = 1d;
            _chargeTarget = 1d;
            _turboLag = 2d;
            _chargeDensity = 0d;

            // set reference conditions
            _power0 = power;
            _rpm0 = speed;

            // Guess at reasonable values for these guys.  Displacements run
            // at about 2 cubic inches per horsepower or so, at least for
            // non-turbocharged engines.
            // change to 1.4; supercharging...
            // (but if passed values, use them instead)
            if (compression > 0d)
                _compression = compression;
            else
                _compression = 8d;

            if (displacement > 0d)
                _displacement = displacement;
            else
                _displacement = power * (1.4d * CIN2CM / HP2W);

            // Create mixture table
            mixtureEfficiency = new FloatCurve();
            /*ConfigNode node = new ConfigNode("MixtureEfficiency");
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
            mixtureEfficiency.Load(node);*/
            mixtureEfficiency.Load(GameDatabase.Instance.GetConfigNodes("AJEPISTONENGINES").FirstOrDefault().GetNode("AFREfficiency"));

            // Set turbo/super params
            SetBoostParams(wastegate, boost0, boost1, rated0, rated1, cost1, switchAlt, turbo);

            // compute volumetric efficiency of engine, based on rated power and BSFC.
            ComputeVEMultiplier(); 
        }

        // gets pressure at given alt in meters, used as fallback for SetBoostParams
        public double GetPressure(double alt)
        {
            if (alt < 11d)
                return 101325d * Math.Pow((1d - 0.0065d * alt / 288.15d), 5.2561);
            else
                return 101325d * Math.Pow((1d - 0.0065 * 11d / 288.15), 5.2561)
                    * Math.Exp(6371000d * 6371000d / ((6371000 + alt) * (6371000 + alt)) * 9.80665d * (11000d - alt) / 287.3d / 329.65d);
        }
        CelestialBody RecurseFindHomeworld(PSystemBody b)
        {
            if (b.celestialBody != null && b.celestialBody.isHomeWorld)
                return b.celestialBody;
            foreach (PSystemBody c in b.children)
            {
                CelestialBody h = RecurseFindHomeworld(c);
                if (h != null)
                    return h;
            }
            return null;
        }
        CelestialBody FindHomeworld()
        {
            CelestialBody body = null;
            
            if(Planetarium.fetch)
                body = Planetarium.fetch.Home;

            if (body == null && FlightGlobals.fetch != null && FlightGlobals.fetch.bodies != null)
                foreach (CelestialBody b in FlightGlobals.fetch.bodies)
                    if (b.isHomeWorld)
                        return b;

            if (body == null && PSystemManager.Instance != null && PSystemManager.Instance.systemPrefab != null)
            {
                body = RecurseFindHomeworld(PSystemManager.Instance.systemPrefab.rootBody);
            }

            return body;
        }
        // set boost parameters:
        // maximum MAP, the two boost pressures to maintain, the two rated altitudes (km),
        // the cost for the second boost mode, and the switch altitude (in km), or -1f for auto
        /// <summary>
        /// Set turbo/supercharger parameters
        /// </summary>
        /// <param name="wastegate">maximum MAP</param>
        /// <param name="boost0">boost mode 1 multiplier</param>
        /// <param name="boost1">boost mode 2 multiplier (or zero for 1-stage)</param>
        /// <param name="rated0">rated altitude in km for mode 1</param>
        /// <param name="rated1">rated altitude in km for mode 2</param>
        /// <param name="cost1">cost in HP for mode 2</param>
        /// <param name="switchAlt"></param>
        /// <param name="turbo"></param>
        /// <returns></returns>
        public bool SetBoostParams(double wastegate, double boost0, double boost1, double rated0, double rated1, double cost1, double switchAlt, bool turbo)
        {
            bool retVal = false;
            double pres0, pres1, switchpres;

            // get pressure at rated alts
            CelestialBody body = FindHomeworld();
            
            if (body != null)
            {
                pres0 = body.GetPressure(rated0) * 1000;
                pres1 = body.GetPressure(rated1) * 1000;
                switchpres = body.GetPressure(switchAlt) * 1000;
            }
            else
            {
                pres0 = GetPressure(rated0);
                pres1 = GetPressure(rated1);
                switchpres = GetPressure(switchAlt);
            }

            if (boost0 > 0d)
            {
                _boostMults[0] = boost0 / pres0;
                _maxMP = wastegate;
                retVal = true;
            }
            if (boost1 > 0d)
            {
                _boostMults[1] = boost1 / pres1;
                _boostCosts[1] = cost1;
            }
            else
            {
                _boostMults[1] = 0d;
                _boostCosts[1] = 0d;
            }
            if (switchAlt >= 0d)
                _boostSwitch = switchpres;
            else
                _boostSwitch = 0d;
            MonoBehaviour.print("*AJE* Setting boost params. MaxMP = " + wastegate + ", boosts = " + _boostMults[0] + "/" + _boostMults[1] + ", switch " + _boostSwitch + " from " + boost0 + "@" + rated0 + ", " + boost1 + "@" + rated1);
            _hasSuper = !turbo && boost0 > 1.0d;
            return retVal;
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
            double efficiency = mixtureEfficiency.Evaluate(fuelAirRatio);
            double MAP = Math.Min(_maxMP, P0 * _boostMults[0]);
            double m_dot_air = GetAirflow(P0, T0, RAIR0, GAMMA0, _rpm0, MAP);
            double m_dot_fuel = fuelAirRatio * m_dot_air;
            double power = m_dot_fuel * efficiency * _bsfcRecip;
            _voleffic = _power0 / power;
            double m_dot_air2 = GetAirflow(P0, T0, RAIR0, GAMMA0, _rpm0, MAP);
            double m_dot_fuel2 = fuelAirRatio * m_dot_air2;
            power = m_dot_fuel2 * efficiency * _bsfcRecip;
            MonoBehaviour.print("*AJE* Setting volumetric efficiency. At SL with MAP " + MAP + ", power = " + power / HP2W + "HP, BSFC = " + _bsfc + ", mda/f = " + m_dot_air2 + "/" + m_dot_fuel2 + ", VE = " + _voleffic + ". Orig a/f: " + m_dot_air + "/" + m_dot_fuel);
        }
        #endregion

        #region Getters/Setters
        /// <summary>
        /// Power delivered to the propeller in W.
        /// </summary>
        /// <returns></returns>
        public double GetShaftPower() { return _power; }

        /// <summary>
        /// Total power generated by the engine in W.
        /// </summary>
        /// <returns></returns>
        public double GetTotalPower() { return _totalPower; }

        /// <summary>
        /// Thrust in kN from the engine itself
        /// </summary>
        /// <returns></returns>
        public double GetEngineThrust() { return _engineThrust; }

        /// <summary>
        /// Fuel flow in kg/sec.
        /// </summary>
        /// <returns></returns>
        public double GetFuelFlow() { return _fuelFlow; }

        /// <summary>
        /// Gets temperature in K (exhaust gas temp).
        /// </summary>
        /// <returns></returns>
        public double GetTemp() { return _egt; }

        /// <summary>
        /// Gets the FX power of the engine (0-1)
        /// </summary>
        /// <returns></returns>
        public float GetFXPower() { return (float)(_mp / Math.Min(29.921d, _maxMP)); }

        /// <summary>
        /// Gets engine status.
        /// </summary>
        /// <returns></returns>
        public string GetStatus() { return _status; }

        /// <summary>
        /// Sets multiplier to engine power.
        /// </summary>
        /// <param name="newMult">new multiplier</param>
        public void SetMultiplier(double newMult) { _volEfficMult = newMult; }

        /// <summary>
        /// Sets mixture
        /// </summary>
        /// <param name="newMix">new value (0-1)</param>
        public void SetMixture(float newMix) { _mixture = newMix; }

        /// <summary>
        /// Sets wastegate (at 1.0, full charge, at 0, ambient)
        /// </summary>
        /// <param name="wastegate">new value (0-1)</param>
        public void SetWastegate(double wastegate) { _wastegate = wastegate; }

        public double GetMAP() { return _mp; }
        public double GetChargeTemp() { return _chargeTemp; }
        #endregion

        #region Update loop etc
        // return the fuel:air ratio of the given mixture setting.
        // mix = range [0, 1]
        public float FuelAirRatio(float mix)
        {
            return 0.052f + 0.07028571f * mix; // prevent an AFR too high or low
        }

        // return the relative volumetric efficiency of the engine, given its compression ratio
        // at the ambient pressure pAmb and the manifold absolute pressure MAP
        public double GetPressureVE(double pAmb, double MAP, double gamma)
        {
            // JSBSim
            return ((gamma - 1f) / gamma) + (_compression - (pAmb / MAP)) / (gamma * (_compression - 1f));
        }

        // return the charge air temperature after heating and cooling (if any)
        // at given manifold absolute pressure MAP, and given ambient pressure and temp
        // Very simple model: the aftercooler will cool the charge to (cooling efficiency) of
        //  ambient temperature, to a minimum temperature of (cooler min)
        public double GetCAT(double MAP, double pAmb, double tAmb)
        {
            // Air entering the manifold does so rapidly, and thus the
            // pressure change can be assumed to be adiabatic.  Calculate a
            // temperature change, and then apply aftercooling/intercooling (if any)
            double T = tAmb * Math.Pow((MAP * MAP) / (pAmb * pAmb), 1d / 7d);
            return Math.Max(_coolerMin, T - (T - tAmb) * _coolerEffic);
        }

        // return the mass airflow through the engine
        // running at given speed in radians/sec, given manifold absolute pressure MAP,
        // given ambient pressure and temperature. Depends on displacement and
        // the volumetric efficiency multiplier.
        public double GetAirflow(double pAmb, double tAmb, double RAir, double gamma, double speed, double MAP)
        {
            //from JSBSim
            // air flow
            double swept_volume = (_displacement * speed * (1d/60d)) * 0.5d;
            double v_dot_air = swept_volume * GetPressureVE(pAmb, MAP, gamma) * _voleffic * _volEfficMult;

            _chargeDensity = MAP / (RAir * GetCAT(MAP, pAmb, tAmb));
            return v_dot_air * _chargeDensity;
        }

        // Gets the target for the [turbo]supercharger
        // takes engine speed, boost mode
        double GetChargeTarget(double speed, int boostMode)
        {
            // Calculate the factor required to modify supercharger output for
            // rpm. Assume that the normalized supercharger output ~= 1 when
            // the engine is at the nominal peak-power rpm.  A power equation
            // of the form (A * B^x * x^C) has been derived empirically from
            // some representative supercharger data.  This provides
            // near-linear output over the normal operating range, with
            // fall-off in the over-speed situation.
            double rpm_norm = (speed / _rpm0);
            double A = 1.795206541d;
            double B = 0.55620178d;
            double C = 1.246708471d;
            double rpm_factor = A * Math.Pow(B, rpm_norm) * Math.Pow(rpm_norm, C);
            return 1d + ((_boostMults[boostMode] - 1d) * rpm_factor);
        }

        double GetCharge(double target, double deltaTime)
        {
            double chg = _charge;
            if (_hasSuper)
            {
                // Superchargers have no lag
                chg = target;
            }
            else //if (!_running)
            {
                // Turbochargers only work well when the engine is actually
                // running.  The 25% number is a guesstimate from Vivian.

                // now clamp near target

                // Also now use when not running -- the point of this is turbo lag!
                double delta = target - _charge;
                if (delta > -0.05d * target && delta < 0.05d * target)
                    chg = target;
                else
                    chg = _charge + delta * 0.25d * deltaTime;
            }
            return chg;
        }

        // return the manifold absolute pressure in pascals
        // takes the ambient pressure and the boost mode
        // clamps to [ambient pressure.....wastegate]
        public double CalcMAP(double pAmb, int boostMode, double charge)
        {
            // We need to adjust the minimum manifold pressure to get a
            // reasonable idle speed (a "closed" throttle doesn't suck a total
            // vacuum in real manifolds).  This is a hack.
            double _minMP = (-0.004d * _boostMults[boostMode]) + _minthrottle;

            double MAP = pAmb * charge;


            // Scale the max MP according to the WASTEGATE control input.  Use
            // the un-supercharged MP as the bottom limit.

            MAP = Math.Min(MAP, Math.Max(_wastegate * _maxMP, pAmb));

            // Scale to throttle setting
            MAP *= _minMP + (1d - _minMP) * _throttle;

            return MAP;
        }

        public double GetEGT(double combustion_efficiency, double T_amb, double Cp_air)
        {
            // using constants from JSB, along with rest of code.
            double enthalpy_exhaust = _fuelFlow * 47.3e6d * combustion_efficiency * 0.30d;
            double heat_capacity_exhaust = (Cp_air * _airFlow) + (1700d * _fuelFlow); // hsp of AvGas in J/kg
            double delta_T_exhaust = enthalpy_exhaust / heat_capacity_exhaust;
            return T_amb + delta_T_exhaust;
        }

        /// <summary>
        /// Update method for piston engine
        /// </summary>
        /// <param name="solver"></param>
        /// <param name="shaftRPM"></param>
        /// <param name="deltaTime">the current delta time for updates</param>
        public void Update(EngineSolver solver, double shaftRPM, double deltaTime)
        {
            double pAmb = solver.p0 + _ramAir * solver.Q;

            _throttle = solver.throttle;
            _fuel = solver.ffFraction > 0d;
            _engineThrust = 0d;
            _status = "Windmilling";
            bool canStart = _running = solver.running;

            // in precedence order, set running = false (highest priority message last)
            if (shaftRPM < 60d) // 60RPM min for engine running
            {
                _status = "Too Low RPM"; // status should never be seen because
                _running = false; // the starter motor bit at the end will happen
                // or another case will trump this.
            }
            if (_throttle == 0d || !canStart)
            {
                _status = "Magnetos Off";
                _running = false;
                canStart = false;
            }
            if (!_fuel)
            {
                _status = "Out of Fuel";
                _running = false;
                canStart = false;
            }
            if (!solver.oxygen || solver.rho <= 0d)
            {
                _status = "No Oxygen";
                _running = false;
                canStart = false;
            }

            if(_running)
            {
                _status = "Nominal";

                shaftRPM = Math.Max(shaftRPM, 30d); // 30RPM min when running to avoid NaNs

                // check if we need to switch boost modes
                double power;
                double MAP;
                float fuelRatio = FuelAirRatio(_mixture);
                double efficiency = mixtureEfficiency.Evaluate(fuelRatio);
                if (_boostSwitch > 0)
                {
                    if (pAmb < _boostSwitch - 1000d && _boostMode < 1)
                        _boostMode++;
                    if (pAmb > _boostSwitch + 1000d && _boostMode > 0)
                        _boostMode--;

                    _chargeTarget = GetChargeTarget(shaftRPM, _boostMode);
                    _charge = GetCharge(_chargeTarget, deltaTime);

                    MAP = CalcMAP(pAmb, _boostMode, _charge);

                    // Compute fuel flow
                    _airFlow = GetAirflow(pAmb, solver.t0, solver.R_c, solver.gamma_c, shaftRPM, MAP);
                    _fuelFlow = _airFlow * fuelRatio;
                    power = _fuelFlow * efficiency * _bsfcRecip - _boostCosts[_boostMode];
                }
                else // auto switch
                {
                    // assume supercharger for now, so charge = target
                    double target0 = GetChargeTarget(shaftRPM, 0);
                    double target1 = GetChargeTarget(shaftRPM, 1);
                    double charge0 = GetCharge(target0, deltaTime);
                    double charge1 = GetCharge(target1, deltaTime);
                    double MAP0 = CalcMAP(pAmb, 0, charge0);
                    double MAP1 = CalcMAP(pAmb, 1, charge1);

                    double airflow0 = GetAirflow(pAmb, solver.t0, solver.R_c, solver.gamma_c, shaftRPM, MAP0);
                    double m_dot_fuel0 = airflow0 * fuelRatio;
                    double power0 = m_dot_fuel0 * efficiency * _bsfcRecip - _boostCosts[0];

                    double airflow1 = GetAirflow(pAmb, solver.t0, solver.R_c, solver.gamma_c, shaftRPM, MAP1);
                    double m_dot_fuel1 = airflow1 * fuelRatio;
                    double power1 = m_dot_fuel1 * efficiency * _bsfcRecip - _boostCosts[1];

                    if (power0 >= power1)
                    {
                        MAP = MAP0;
                        _chargeTarget = target0;
                        _charge = charge0;
                        power = power0;
                        _airFlow = airflow0;
                        _fuelFlow = m_dot_fuel0;
                        _boostMode = 0;
                    }
                    else
                    {
                        MAP = MAP1;
                        _chargeTarget = target1;
                        _charge = charge1;
                        power = power1;
                        _airFlow = airflow1;
                        _fuelFlow = m_dot_fuel1;
                        _boostMode = 1;
                    }
                }

                _mp = MAP;
                _chargeTemp = GetCAT(MAP, pAmb, solver.t0); // duplication of effort, but oh well

                // The "boost" is the delta above ambient
                _boostPressure = _mp - pAmb;

                _power = power;
                _totalPower = _power + _boostCosts[_boostMode];
                _egt = GetEGT(efficiency, solver.t0, solver.Cp_c);

                // Additional thrust from engine
                // FIXME now that we calculate EGT should use that.

                // heat from mixture, heat hack for over-RPM, multiply by relative manifold pressure
                double tmpRatio = shaftRPM / _rpm0;
                if (tmpRatio > 1d)
                    tmpRatio *= tmpRatio;
                float mixRatio = (1.5f - _mixture);
                mixRatio *= mixRatio;
                mixRatio = (mixRatio + 0.2f) * 1.2f;
                double tempDelta = tmpRatio * mixRatio * _mp / _maxMP;
                

                // exhaust thrust, normalized to "10% HP in lbf"
                if (_exhaustThrust != 0d)
                {
                    _netExhaustThrust = _exhaustThrust * (_totalPower * W2HP * 0.1d * LBFTON) * tempDelta;
                    _engineThrust += _netExhaustThrust;
                }

                // Meredith Effect radiator thrust, scaled by Q and by how hot the engine is running and the ambient temperature
                // CTOK is there because tempdelta is expressed as a multiple of 0C-in-K (FIXME)
                if (_meredithEffect != 0d)
                {
                    _netMeredithEffect = _meredithEffect * solver.Q * (tempDelta * 273.15d / solver.t0);
                    _engineThrust += _netMeredithEffect;
                }

                //MonoBehaviour.print("Engine running: HP " + power * W2HP + "/" + _totalPower * W2HP + ", rpm " + shaftRPM + ", mp " + _mp + ", charge " + _charge + ", cat " + _chargeTemp + ", chargeRho " + _chargeDensity + ", egt " + _egt + ", fuel " + _fuelFlow + ", airflow " + _airFlow + ", effic " + efficiency);
            }
            else
            {
                _mp = solver.p0;
                _boostPressure = 0d;
                // assume a starter motor of 15% power
                if (canStart)
                {
                    _status = "Starter On";
                    _power = _totalPower = 0.2d * _power0;
                    _fuelFlow = _power * _bsfc * 0.2d;
                    _airFlow = _fuelFlow * 10d;
                    _mp = 0.15d * _maxMP;
                    _egt = GetEGT(1d, solver.t0, solver.Cp_c);
                }
                else
                {
                    _fuelFlow = _airFlow = _power = _totalPower = 0d;
                    if (_egt > solver.t0 + 1d)
                        _egt += (solver.t0 - _egt) * 0.05d * deltaTime;
                    else
                        _egt = solver.t0;
                }
            }

            // unused
            /*
            // Oil temperature.
            // Assume a linear variation between ~90degC at idle and ~120degC
            // at full power.  No attempt to correct for airflow over the
            // engine is made.  Make the time constant to attain target steady-
            // state oil temp greater at engine off than on to reflect no
            // circulation.  Nothing fancy, but populates the guage with a
            // plausible value.
            double tau;	// secs 
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
            _dOilTempdt = (_oilTempTarget - _oilTemp) / tau;*/
        }
        #endregion

        #region Debug
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
        #endregion
    }
}
