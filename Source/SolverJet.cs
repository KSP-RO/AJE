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



        //Total mass flow through main nozzle (fuel + air)
        private double mdot;

        //Fuel heat of burning and peak temperatures
        private double h_f, Tt4, Tt7;

        //Reference area of the engine, combustor, and nozzle
        private double Aref, Acomb, Anozzle;
        private double spoolFactor;

        //use exhaust mixer or not
        private bool exhaustMixer;

        // engine status
        protected bool combusting = true;

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
            bool useExhaustMixer
            )
        {

            Aref = Area;
            spoolFactor = 1d - 0.05 * Math.Min(1d / Aref, 9d);
            BPR = bypassRatio; inv_BPRp1 = 1d / (1d + BPR);
            CPR = compressorRatio;
            FRC = BPR / CPR;
            FPR = fanRatio;
            M_d = designMach;
            T_d = designTemperature;
            eta_c = compressorEta; inv_eta_c = 1d / eta_c;
            eta_t = turbineEta;
            eta_n = nozzleEta;
            h_f = heatOfFuel;
            Tt4 = max_TIT;
            Tt7 = max_TAB;
            exhaustMixer = useExhaustMixer;

            //calculate TTR at design point first
            th0.FromStandardConditions(false);
            th0.T = T_d;
            th1.FromChangeReferenceFrameMach(th0, M_d);
            // Note that this work is negative
            // Different mass flows between compressor, turbine, and bypass automatically taken care of by MassRatio
            double turbineWork = 0d;
            if (BPR > 0d)
                turbineWork += th2.FromAdiabaticProcessWithPressureRatio(th1, FPR, efficiency: eta_c);
            turbineWork += th3.FromAdiabaticProcessWithPressureRatio(th1, CPR, efficiency: eta_c);
            th4.FromAddFuelToTemperature(th3, Tt4, h_f);
            th5.FromAdiabaticProcessWithWork(th4, turbineWork, efficiency: eta_t);
            TTR = th5.T / th4.T;

        }

        public void UpdateArea(double newArea)
        {
            Aref = newArea;
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
            else if(ffFraction <= 0d)
            {
                combusting = false;
                statusString = "No fuel";
            }
            else if(CPR == 1 && M0 < 0.3d)
            {
                combusting = false;
                statusString = "Below ignition speed";
            }
            else if(airRatio < 0.01d)
            {
                combusting = false;
                statusString = "Insufficient intake area";
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
                for (int i = 0; i < 20; i++)    //use iteration to calculate CPR
                {
                    th3.FromAdiabaticProcessWithPressureRatio(th1, prat3, efficiency: eta_c);
                    // FIXME use ffFraction here? Instead of just multiplying thrust by fuel fraction in the module?
                    // is so, set multiplyThrustByFuelFrac = false in the ModuleEnginesAJEJet.
                    th4.FromAddFuelToTemperature(th3, Tt4, h_f, throttle: mainThrottle);
                    double turbineWork = th5.FromAdiabaticProcessWithTempRatio(th4, TTR, eta_t);

                    double x = prat3;

                    prat3 = turbineWork / th1.T / th1.Cp + 1 + BPR;
                    prat3 /= 1 + BPR * p;
                    prat3 = Math.Pow(prat3, eta_c * th1.Gamma / (th1.Gamma - 1.0));

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

                double p8, V8, A8;
                double epr = th7.P / th1.P;
                double etr = th7.T / th1.T;

                // The factor of 0.75 implies that the mach number of air entering the compressor is about 0.5
                // The factor can be calculated as 
                // sqrt(gamma_c/gamma_ab * R_ab / R_c) * M * (1 + (gamma_c + 1)/2 * M^2)^(-(gamma_c + 1)/(2 * (gamma_c - 1)) / (gamma_ab + 1 / 2)^(-(gamma_ab + 1)/(2 * (gamma_ab - 1))
                double area8max = .75 * Math.Sqrt(etr) / epr;//ratio of nozzle area to ref area
                A8 = area8max * Aref;
                if (exhaustMixer && BPR > 0)
                    A8 *= (1 + BPR);
                double eair = th7.P * Math.Sqrt(th7.Gamma / th7.R / th7.T) *
                    Math.Pow((.5 + .5 * th7.Gamma), .5 * (1 + th7.Gamma) / (1 - th7.Gamma));//corrected mass flow per area
                mdot = eair * A8;
                double npr = th7.P / th0.P;
                double fac1 = (th7.Gamma - 1.0) / th7.Gamma;
                double exitEnergy = th7.R / fac1 * th7.T * eta_n * (1.0 - Math.Pow(1.0 / npr, fac1));
                V8 = Math.Sign(exitEnergy) * Math.Sqrt(Math.Abs(2.0 * exitEnergy));     //exit velocity - may be negative under certain conditions
                Anozzle = mdot / th7.P * th7.R * th7.T / V8 * Math.Pow(0.5 * V8 * V8 / th7.Cp / th7.T + 1d, 0.5d * (th7.Gamma + 1d) / (th7.Gamma - 1d));
                double chokeRatio = Math.Pow(0.5d * (th7.Gamma + 1d), th7.Gamma / (th7.Gamma - 1d));
                p8 = (npr <= chokeRatio) ? th0.P : th7.P / chokeRatio;
                thrust = V8 * mdot + (p8 - th0.P) * Anozzle;
                thrust -= mdot * (1.0 - th7.FF) * (vel);//ram drag

                if (BPR > 0 && FPR > 1 && exhaustMixer == false)
                {
                    fac1 = (th2.Gamma - 1) / th2.Gamma; //fan thrust from NASA
                    double snpr = th2.P / th0.P;
                    double fExitEnergy = th2.R / fac1 * th2.T * eta_n * (1.0 - Math.Pow(1.0 / snpr, fac1));
                    double ues = Math.Sign(fExitEnergy) * Math.Sqrt(Math.Abs(2.0 * fExitEnergy));
                    double bypassAirFlow = mdot / th7.MassRatio * th2.MassRatio; // th2.MassRatio will be equal to BPR at this point
                    double ANozzleBypass = bypassAirFlow / th2.P * th2.R * th2.T / ues * Math.Pow(0.5 * ues * ues / th2.Cp / th2.T + 1d, 0.5d * (th2.Gamma + 1d) / (th2.Gamma - 1d));
                    chokeRatio = Math.Pow(0.5d * (th2.Gamma + 1d), th2.Gamma / (th2.Gamma - 1d));
                    double pfexit = (snpr <= chokeRatio) ? th0.P : th2.P / chokeRatio; //exit pressure of fan
                    thrust += bypassAirFlow * ues + (pfexit - p0) * ANozzleBypass;
                    thrust -= bypassAirFlow * vel;
                }

                thrust *= flowMult * ispMult;
                fuelFlow = mdot * th7.FF * flowMult;
                Isp = thrust / (fuelFlow * 9.80665);
                thrust *= airRatio; // FIXME: should this get applied to fuel flow and Isp too?

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

        public void FitEngine(ModuleEnginesAJEJet engineModule)
        {
            float TPR = AJEInlet.OverallStaticTPR(engineModule.defaultTPR);
            SetEngineState(true, 1d);
            SetStaticConditions(overallTPR : TPR);
            double throttle = Tt7 > 0d ? 2d / 3d : 1d;
            CalculatePerformance(1d, throttle, 1d, 1d);

            System.Diagnostics.Debug.Assert(engineModule.Area == Aref);
            Aref *= engineModule.dryThrust * 1000d / thrust;

            engineModule.Area = (float)Aref;
        }

        public override double GetEngineTemp() { return th3.T; }
        public override double GetArea() { return Aref * (1d + BPR); }
        public override double GetEmissive() { return fxPower; }
        public override float GetFXPower() { return fxPower; }
        public override float GetFXRunning() { return fxPower; }
        public override float GetFXThrottle() { return fxPower; }
        public override float GetFXSpool() { return fxPower; }
        public override bool GetRunning() { return combusting; }

        public double Prat3 { get { return prat3; } }
    }

}
