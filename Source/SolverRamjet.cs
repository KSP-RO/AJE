using System;
using SolverEngines;

namespace AJE
{
    public class SolverRamjet : EngineSolver
    {
        //conditions at burner rear / nozzle entrance;
        private double eta_n;
        private EngineThermodynamics th7 = new EngineThermodynamics();

        //conditions at nozzle throat
        private double p8, V8;

        protected float fxPower = 0f;
        
        //engine design point; mach number, temperature 
        private double M_d, T_d;
        
        //Total mass flow through main nozzle (fuel + air), air flow through core
        private double mdot, coreAirflow;

        //Fuel heat of burning
        private double h_f;

        //Max fuel to air ratio
        private double maxFar;

        //Reference area of the engine, throat, and nozzle exit
        private double Aref, Athroat, Anozzle;

        //Whether the nozzle is adjustable to accelerate exhaust to supersonic speed
        private bool adjustableNozzle;

        // engine status
        protected bool combusting = true;

        //---------------------------------------------------------
        //Initialization Functions

        public void InitializeOverallEngineData(
            double Area,
            double designMach,
            double designTemperature,
            double nozzleEta,
            double heatOfFuel,
            double maxFar,
            bool adjustableNozzle
            )
        {

            Aref = Area;
            M_d = designMach;
            T_d = designTemperature;
            eta_n = nozzleEta;
            h_f = heatOfFuel;
            this.maxFar = maxFar;
            this.adjustableNozzle = adjustableNozzle;

            InitializeEngine();
        }

        protected void InitializeEngine()
        {
            //calculate TTR at design point first

            // Don't overwrite th0 and th1
            EngineThermodynamics ambientTherm = EngineThermodynamics.StandardConditions(false);
            ambientTherm.T = T_d;
            EngineThermodynamics inletTherm = ambientTherm.ChangeReferenceFrameMach(M_d);
            
            th7 = inletTherm.AddFuelToTemperature(9999d, h_f, throttle: throttle, maxFar: maxFar);
            th7.P *= 0.95;

            double massFlow = inletTherm.CalculateMassFlow(Aref, 0.5d);
            Athroat = th7.CalculateFlowArea(massFlow * th7.MassRatio, 1d);

            if (!adjustableNozzle)
            {
                p8 = ambientTherm.P;
                CalculateANozzle();
            }
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
            else if (th1.P * 0.9 < th0.P)
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
                th7 = th1.AddFuelToTemperature(9999d, h_f, throttle: throttle, maxFar: maxFar);
                th7.P *= 0.95;

                //Nozzle code is from NASA
                
                //double A8;
                double epr = th7.P / th1.P;
                double etr = th7.T / th1.T;
                
                coreAirflow = th7.CalculateMassFlow(Athroat, 1d) / th7.MassRatio;
                mdot = th7.MassRatio * coreAirflow;

                if (adjustableNozzle)
                {
                    p8 = th0.P;
                    double exitEnergy = th7.Cp * th7.T * eta_n * (1.0 - Math.Pow(p8 / th7.P, th7.R / th7.Cp));
                    V8 = Math.Sqrt(Math.Abs(2d * exitEnergy));     //exit velocity - may be negative under certain conditions
                    V8 *= Math.Sign(exitEnergy);
                    double exitMach = th7.CalculateMach(Math.Abs(V8));
                    Anozzle = th7.CalculateFlowArea(mdot, exitMach);
                }

                thrust = V8 * mdot + (p8 - th0.P) * Anozzle;
                thrust -= mdot * (1d - th7.FF) * (vel);//ram drag

                thrust *= flowMult * ispMult;
                fuelFlow = mdot * th7.FF * flowMult;
                Isp = thrust / (fuelFlow * 9.80665);
                SFC = 3600d / Isp;
            }
            // Set FX power
            fxPower = (float)throttle;
        }

        // Be sure to set th7, eta_n, p8
        // Sets Anozzle and V8
        private void CalculateANozzle()
        {
            double exitEnergy = th7.Cp * th7.T * eta_n * (1.0 - Math.Pow(p8 / th7.P, th7.R / th7.Cp));
            V8 = Math.Sqrt(Math.Abs(2d * exitEnergy));
            V8 *= Math.Sign(exitEnergy);
            double exitMach = th7.CalculateMach(Math.Abs(V8));
            Anozzle = th7.CalculateFlowArea(mdot, exitMach);
        }

        public override double GetEngineTemp() { return th7.T; }
        public override double GetArea() { return Aref; }
        public override double GetEmissive() { return fxPower; }
        public override float GetFXPower() { return (float)fxPower; }
        public override float GetFXRunning() { return fxPower; }
        public override float GetFXThrottle() { return fxPower; }
        public override float GetFXSpool() { return (float)fxPower; }
        public override bool GetRunning() { return combusting; }
        
        public double GetExhaustTemp() { return th7.T; }
        public double GetNozzleArea() { return Anozzle; }

        public double GetAref() { return Aref; }
        public double GetFHV() { return h_f; }
    }
}
