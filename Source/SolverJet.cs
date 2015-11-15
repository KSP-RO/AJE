using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;
using SolverEngines;


namespace AJE
{
    public class SolverJet : EngineSolver
    {

        //overall engine design parameters; bypass ratio, fan pressure ratio, 
        //compressor pressure ratio, turbine temperature ratio, 
        private double BPR, FPR, CPR, TTR, inv_BPRp1;

        //fan ratio constant = FPR/CPR
        private double FRC;

        //engine design point; mach number, temperature 
        private double M_d, T_d;

        //pressure ratios
        private double prat2, prat3;

        //conditions at fan; pressure, temperature;
        private EngineThermodynamics th2 = new EngineThermodynamics();

        //Conditions at burner inlet / compressor exit
        private double eta_c, inv_eta_c;
        private EngineThermodynamics th3 = new EngineThermodynamics();

        //conditions at burner exit / turbine entrance; pressure, temperature, mass flow rate
        private double eta_t;
        private EngineThermodynamics th4 = new EngineThermodynamics();

        //conditions at ab inlet / turbine exit
        private EngineThermodynamics th5 = new EngineThermodynamics();

        //conditions at ab exhaust mixer;
        private EngineThermodynamics th6 = new EngineThermodynamics();

        //conditions at ab rear / nozzle entrance;
        private double eta_n;
        private EngineThermodynamics th7 = new EngineThermodynamics();

        //Throttles for burner and afterburner
        protected double mainThrottle, abThrottle;

        protected float fxPower = 0f;



        //Total mass flow through main nozzle (fuel + air), air flow through core
        private double mdot, coreAirflow;

        //Fuel heat of burning and peak temperatures
        private double h_f, Tt4, Tt7;

        //Reference area of the engine, combustor, and nozzle
        private double Aref, Acomb, Anozzle;
        private double spoolFactor;

        //use exhaust mixer or not
        private bool exhaustMixer;

        //Whether the nozzle is adjustable to accelerate exhaust to supersonic speed
        private bool adjustableNozzle;

        // engine status
        protected bool combusting = true;

        // Engine fit data
        protected double dryThrust, drySFC, wetThrust;

        //---------------------------------------------------------
        //Initialization Functions

        public void InitializeOverallEngineData(
            double Area,
            double bypassRatio,
            double compressorRatio,
            double fanRatio,
            double designMach,
            double designTemperature,
            double compressorEta,
            double turbineEta,
            double nozzleEta,
            double heatOfFuel,
            double max_TIT,
            double max_TAB,
            bool useExhaustMixer,
            bool supersonicNozzle
            )
        {

            Aref = Area;
            spoolFactor = 1d - 0.05 * Math.Min(1d / Aref, 9d);
            BPR = bypassRatio; inv_BPRp1 = 1d / (1d + BPR);
            CPR = compressorRatio;
            FPR = fanRatio;
            FRC = FPR / CPR;
            M_d = designMach;
            T_d = designTemperature;
            eta_c = compressorEta; inv_eta_c = 1d / eta_c;
            eta_t = turbineEta;
            eta_n = nozzleEta;
            h_f = heatOfFuel;
            Tt4 = max_TIT;
            Tt7 = max_TAB;
            exhaustMixer = useExhaustMixer;
            adjustableNozzle = supersonicNozzle;

            CalculateTTR();
        }

        protected void CalculateTTR()
        {
            //calculate TTR at design point first

            // Don't overwrite th0 and th1
            EngineThermodynamics ambientTherm = new EngineThermodynamics();
            EngineThermodynamics inletTherm = new EngineThermodynamics();

            ambientTherm.FromStandardConditions(false);
            ambientTherm.T = T_d;
            inletTherm.FromChangeReferenceFrameMach(ambientTherm, M_d);
            // Note that this work is negative
            // Different mass flows between compressor, turbine, and bypass automatically taken care of by MassRatio
            double turbineWork = th3.FromAdiabaticProcessWithPressureRatio(inletTherm, CPR, efficiency: eta_c);
            if (BPR > 0d)
                turbineWork += th2.FromAdiabaticProcessWithPressureRatio(inletTherm, FPR, efficiency: eta_c) * BPR;
            th4.FromAddFuelToTemperature(th3, Tt4, h_f);
            th5.FromAdiabaticProcessWithWork(th4, turbineWork, efficiency: eta_t);
            TTR = th5.T / th4.T;
        }

        public override void CalculatePerformance(double airRatio, double commandedThrottle, double flowMult, double ispMult)
        {
            // set base bits
            base.CalculatePerformance(airRatio, commandedThrottle, flowMult, ispMult);
            
            // if we're not combusting, don't combust and start cooling off
            combusting = running;
            statusString = "Nominal";
            if (running && (!oxygen || eair0 <= 0d))
            {
                combusting = false;
                statusString = "No oxygen";
            }
            else if (ffFraction <= 0d)
            {
                combusting = false;
                statusString = "No fuel";
            }
            else if (CPR == 1 && M0 < 0.3d)
            {
                combusting = false;
                statusString = "Below ignition speed";
            }
            else if (airRatio < 0.01d)
            {
                combusting = false;
                statusString = "Insufficient intake area";
            }
            else if (underwater)
            {
                combusting = false;
                statusString = "Nozzle in water";
            }

            if (combusting)
            {

                // set throttle
                if (Tt7 == 0)
                {
                    mainThrottle = commandedThrottle;
                }
                else
                {
                    mainThrottle = Math.Min(commandedThrottle * 1.5d, 1.0);
                    abThrottle = Math.Max(commandedThrottle * 3d - 2d, 0);
                }

                prat3 = CPR;
                double p = Math.Pow(FRC, (th1.Gamma - 1) * inv_eta_c / th1.Gamma);
                double invfac = eta_c * th1.Gamma / (th1.Gamma - 1.0);
                for (int i = 0; i < 20; i++)    //use iteration to calculate CPR
                {
                    th3.FromAdiabaticProcessWithPressureRatio(th1, prat3, efficiency: eta_c);
                    // FIXME use ffFraction here? Instead of just multiplying thrust by fuel fraction in the module?
                    // is so, set multiplyThrustByFuelFrac = false in the ModuleEnginesAJEJet.
                    th4.FromAddFuelToTemperature(th3, Tt4, h_f, throttle: mainThrottle);
                    double turbineWork = th5.FromAdiabaticProcessWithTempRatio(th4, TTR, eta_t);

                    double x = prat3;

                    prat3 = turbineWork / th1.T / th1.Cp + 1d + BPR;
                    prat3 /= 1d + BPR * p;
                    prat3 = Math.Pow(prat3, invfac);

                    if (Math.Abs(x - prat3) < 0.01)
                        break;
                }

                if (BPR > 0d)
                {
                    prat2 = FRC * prat3;
                    th2.FromAdiabaticProcessWithPressureRatio(th1, prat2, efficiency: eta_c);
                    th2.MassRatio = BPR;
                }

                if (exhaustMixer && BPR > 0)//exhaust mixer
                {
                    th2.P *= 0.98;
                    th6.FromMixStreams(th5, th2);
                }
                else
                {
                    th6.CopyFrom(th5);
                }

                if (Tt7 > 0)
                {
                    th7.FromAddFuelToTemperature(th6, Tt7, h_f, throttle: abThrottle);
                }
                else
                {
                    th7.CopyFrom(th6);
                }

                //Nozzle code is from NASA

                double p8, V8;
                //double A8;
                double epr = th7.P / th1.P;
                double etr = th7.T / th1.T;

                // The factor of 0.75 implies that the mach number of air entering the compressor is about 0.5
                // The factor can be calculated as 
                // 1 / (1 - ff_ab) * sqrt(gamma_c/gamma_ab * R_ab / R_c) * M * (1 + (gamma_c + 1)/2 * M^2)^(-(gamma_c + 1)/(2 * (gamma_c - 1)) / ((gamma_ab + 1) / 2)^(-(gamma_ab + 1)/(2 * (gamma_ab - 1))
                //
                //A8 = .75 * Aref * th7.MassRatio * Math.Sqrt(th1.Gamma / th7.Gamma * th7.R / th1.R) * Math.Sqrt(etr) / epr;//ratio of nozzle area to ref area
                //double area8max = .75 * Math.Sqrt(etr) / epr;//ratio of nozzle area to ref area
                //A8 = area8max * Aref;
                //if (exhaustMixer && BPR > 0)
                //    A8 *= (1 + BPR);
                //double eair = th7.P * Math.Sqrt(th7.Gamma / th7.R / th7.T) *
                //    Math.Pow((0.5d + 0.5d * th7.Gamma), 0.5d * (1d + th7.Gamma) / (1d - th7.Gamma));//corrected mass flow per area
                //mdot = eair * A8;

                // New way - make M=0.5 explicit
                // Later will be adjusted by prat3
                double compressorEntryMach = 0.5;
                coreAirflow = th1.CalculateMassFlow(Aref, compressorEntryMach);
                mdot = th7.MassRatio * coreAirflow;

                if (!adjustableNozzle)
                {
                    p8 = th7.ChokedPressure();
                    if (p8 < th0.P)
                        p8 = th0.P;
                }
                else
                {
                    p8 = th0.P;
                }
                double exitEnergy = th7.Cp * th7.T * eta_n * (1.0 - Math.Pow(p8 / th7.P, th7.R / th7.Cp));
                V8 = Math.Sqrt(Math.Abs(2d * exitEnergy));     //exit velocity - may be negative under certain conditions
                V8 *= Math.Sign(exitEnergy);
                Anozzle = th7.CalculateFlowArea(mdot, th7.CalculateMach(Math.Abs(V8)));
                thrust = V8 * mdot + (p8 - th0.P) * Anozzle;
                thrust -= mdot * (1d - th7.FF) * (vel);//ram drag

                if (BPR > 0d && FPR > 1d && exhaustMixer == false)
                {
                    //fan thrust from NASA
                    double pfexit = th2.ChokedPressure();
                    if (pfexit < th0.P)
                        pfexit = th0.P;
                    double fExitEnergy = th2.Cp * th2.T * eta_n * (1d - Math.Pow(pfexit / th2.P, th2.R / th2.Cp));
                    double ues = Math.Sqrt(Math.Abs(2d * fExitEnergy));
                    ues *= Math.Sign(fExitEnergy);
                    double bypassAirFlow = coreAirflow * th2.MassRatio; // th2.MassRatio will be equal to BPR at this point
                    double ANozzleBypass = th2.CalculateFlowArea(bypassAirFlow, th2.CalculateMach(ues));
                    thrust += bypassAirFlow * ues + (pfexit - p0) * ANozzleBypass;
                    thrust -= bypassAirFlow * vel;
                }

                thrust *= flowMult * ispMult;
                fuelFlow = mdot * th7.FF * flowMult;
                Isp = thrust / (fuelFlow * 9.80665);
                SFC = 3600d / Isp;

                /*  
                  debugstring = "";
                  debugstring += "TTR:\t" + TTR.ToString("F3") + "\r\n";
                  debugstring += "CPR:\t" + prat3.ToString("F3") + "\r\n"; ;
                  debugstring += "p0: " + th0.P.ToString("F2") + "\tt0: " + th0.T.ToString("F2") + "\r\n";
                  debugstring += "P1: " + th1.P.ToString("F2") + "\tT1: " + th1.T.ToString("F2") + "\r\n";
                  debugstring += "P2: " + th2.P.ToString("F2") + "\tT2: " + th2.T.ToString("F2") + "\r\n";
                  debugstring += "P3: " + th3.P.ToString("F2") + "\tT3: " + th3.T.ToString("F2") + "\r\n";
                  debugstring += "P4: " + th4.P.ToString("F2") + "\tT4: " + th4.T.ToString("F2") + "\r\n";
                  debugstring += "P5: " + th5.P.ToString("F2") + "\tT5: " + th5.T.ToString("F2") + "\r\n";
                  debugstring += "P6: " + th6.P.ToString("F2") + "\tT6: " + th6.T.ToString("F2") + "\r\n";
                  debugstring += "P7: " + th7.P.ToString("F2") + "\tT7: " + th7.ToString("F2") + "\r\n";
                  debugstring += "EPR: " + epr.ToString("F2") + "\tETR: " + etr.ToString("F2") + "\r\n";

                  debugstring += "FF: " + th5.FF.ToString("P") + "\t";
                  debugstring += "FF_AB: " + th7.FF.ToString("P") + "\r\n";
                  debugstring += "V8: " + V8.ToString("F2") + "\tA8: " + A8.ToString("F2") + "\r\n";
                  debugstring += "Thrust: " + (thrust / 1000).ToString("F1") + "\tmdot: " + mdot.ToString("F2") + "\r\n";
                  debugstring += "Isp: " + Isp.ToString("F0") + "\tSFC: " + (3600 / Isp).ToString("F3") + "\r\n";
                  Debug.Log(debugstring);*/
            }
            else
            {
                double shutdownScalar = Math.Pow(spoolFactor, TimeWarp.fixedDeltaTime);

                th3.T = Math.Max(t0, th3.T * shutdownScalar - 4d);

                mainThrottle = Math.Max(0d, mainThrottle * shutdownScalar - 0.05d);
                if (Tt7 > 0)
                    abThrottle = Math.Max(0d, abThrottle * shutdownScalar - 0.05d);
            }
            // Set FX power
            if (Tt7 == 0)
                fxPower = (float)mainThrottle;
            else
                fxPower = (float)(mainThrottle * 0.25d + abThrottle * 0.75d);
        }

        public void SetFitParams(double area, double fhv, double TAB)
        {
            h_f = fhv;
            Aref = area;
            Tt7 = TAB;
            CalculateTTR();
        }

        public void FitEngine(double dryThrust, double drySFC, double wetThrust, double defaultTPR = 1d)
        {
            float TPR = AJEInlet.OverallStaticTPR((float)defaultTPR);
            SetEngineState(true, 1d);
            SetStaticConditions(usePlanetarium: false, overallTPR : TPR);

            double dryThrottle = Tt7 > 0d ? 2d / 3d : 1d;

            this.drySFC = drySFC;
            if (drySFC > 0d)
            {
                CalculatePerformance(1d, dryThrottle, 1d, 1d);
                if (Math.Abs(SFC / drySFC - 1d) > 0.0001d)
                {
                    h_f = SolverMathUtil.BrentsMethod(DrySFCFittingFunction, 10e6, 200e6, maxIter: 1000);
                    CalculateTTR();
                }
            }

            this.dryThrust = dryThrust;
            if (dryThrust > 0d)
            {
                CalculatePerformance(1d, dryThrottle, 1d, 1d);

                Aref *= dryThrust / thrust;
            }
            this.wetThrust = wetThrust;
            if (wetThrust > 0d)
            {
                bool doFit = true;
                double oldTt7 = Tt7;

                if (Tt7 <= 0d)
                    Tt7 = 2500d;
                else
                {
                    CalculatePerformance(1d, 1d, 1d, 1d);
                    if (Math.Abs(thrust/wetThrust - 1d) <= 0.0001d)
                    {
                        // TAB already correct, no need to fit
                        doFit = false;
                    }
                }

                // Check bounds
                if (doFit)
                {
                    CalculatePerformance(1d, 2d / 3d, 1d, 1d);

                    if (thrust >= wetThrust)
                    {
                        Debug.LogWarning("Cannot fit wet thrust on engine because dry thrust is already greater than specified value.");
                        doFit = false;
                    }

                    Tt7 = 4000d;
                    CalculatePerformance(1d, 1d, 1d, 1d);
                    if (thrust <= wetThrust)
                    {
                        Debug.LogWarning("Cannot fit wet thrust on engine solver because it would require an afterburner temperature of more than 4000 K.");
                        doFit = false;
                    }
                }

                if (doFit)
                {
                    Tt7 = SolverMathUtil.BrentsMethod(WetThrustFittingFunction, th5.T, 4000d, maxIter: 1000);
                }
                else
                {
                    Tt7 = oldTt7;
                }
            }
        }

        private double DrySFCFittingFunction(double heatOfFuel)
        {
            h_f = heatOfFuel;
            CalculateTTR();
            CalculatePerformance(1d, Tt7 > 0d ? 2d / 3d : 1d, 1d, 1d);
            return SFC - drySFC;
        }

        private double WetThrustFittingFunction(double TAB)
        {
            Tt7 = TAB;
            CalculatePerformance(1d, 1d, 1d, 1d);
            return thrust - wetThrust;
        }

        public override double GetEngineTemp() { return th3.T; }
        public override double GetArea() { return Aref * (1d + BPR); }
        public override double GetEmissive() { return fxPower; }
        public override float GetFXPower() { return (float)abThrottle; }
        public override float GetFXRunning() { return fxPower; }
        public override float GetFXThrottle() { return fxPower; }
        public override float GetFXSpool() { return (float)mainThrottle; }
        public override bool GetRunning() { return combusting; }

        public double GetPrat3() { return prat3; }
        public double GetT7() { return th7.T; }
        public double GetNozzleArea() { return Anozzle; }

        public double GetAref() { return Aref; }
        public double GetFHV() { return h_f; }
        public double GetTAB() { return Tt7; }
    }

}
