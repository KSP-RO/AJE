using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;



namespace AJE
{
    public class EngineSolver
    {
        //freestream flight conditions; static pressure, static temperature, and mach number
        protected double p0, t0, M0 = 0;

        //inlet total pressure recovery
        protected double TPR;

        //gas properties at start
        protected double gamma_c;
        protected double R_c;
        protected double Cp_c;
        protected double Cv_c;

        //Throttles for burner and afterburner
        protected double mainThrottle, abThrottle;

        //thrust and Isp and fuel flow of the engine
        protected double thrust, Isp, fuelFlow;

        public string debugstring;
        //---------------------------------------------------------
        //Initialization Functions




        virtual public void CalculatePerformance(double pressure, double temperature, double velocity, double airRatio, double commandedThrottle)
        {
            fuelFlow = 0d;
            Isp = 0d;
            thrust = 0d;
        }
        
        // getters for base fields
        public void SetTPR(double t) { TPR = t; }
        public double GetThrust() { return thrust; }
        public double GetIsp() { return Isp; }
        public double GetFuelFlow() { return fuelFlow; }
        public double GetM0() { return M0; }

        // virtual getters
        virtual public double GetEngineTemp() { return 288.15d; }
        virtual public double GetArea() { return 0d; }
        virtual public bool CanThrust() { return true; }


        protected double CalculateGamma(double temperature, double fuel_fraction)
        {
            double gamma = 1.4 - 0.1 * Math.Max((temperature - 300) * 0.0005, 0) * (1 + fuel_fraction);
            gamma = Math.Min(1.4, gamma);
            gamma = Math.Max(1.1, gamma);
            return gamma;
        }

        protected double CalculateCp(double temperature, double fuel_fraction)
        {
            double Cp = 1004.5 + 250 * Math.Max((temperature - 300) * 0.0005, 0) * (1 + 10 * fuel_fraction);
            Cp = Math.Min(1404.5, Cp);
            Cp = Math.Max(1004.5, Cp);
            return Cp;
        }

    }

}
