using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SolverEngines;

namespace AJE
{
    public class SolverRotor : EngineSolver
    {
        private float omega, r, weight, power0, rho0;
        private float buff, BSFC;
        //private double tilt;
        private double ldr, torque;
        protected double vx, vz;
        protected double power;

        private bool useOxygen = true;

        private bool combusting = false;

        public SolverRotor(float omega, float r, float weight, float power0, float rho0, float buff, float BSFC, bool useOxygen=true)
        {
            this.omega = omega;
            this.r = r;
            this.weight = weight;
            this.power0 = power0;
            this.rho0 = rho0;
            this.buff = buff;
            this.BSFC = BSFC;
            this.useOxygen = useOxygen;

            ldr = 3d * r * 9.80665d * weight * omega / 4d / power0;
        }

        public void UpdateFlightParams(float vx, float vz)
        {
            this.vx = vx;
            this.vz = vz;
        }

        public override void CalculatePerformance(double airRatio, double commandedThrottle, double flowMult, double ispMult)
        {
            // set base bits
            base.CalculatePerformance(airRatio, commandedThrottle, flowMult, ispMult);

            statusString = "Nominal";
            combusting = running;
            if (running && ((useOxygen && !oxygen) || eair0 <= 0d))
            {
                combusting = false;
                statusString = "No oxygen";
            }
            else if (ffFraction <= 0d)
            {
                combusting = false;
                statusString = "No fuel";
            }
            else if (underwater)
            {
                combusting = false;
                statusString = "Underwater";
            }

            if (combusting)
            {
                power = power0 * Math.Min(1d, rho / rho0) * throttle;

                double x = vz * weight * 9.80665d / power;
                torque = power / omega / Math.Pow((x / buff + 1d), buff);

                double CLift = omega * omega * r * r * r / 3d + vx * vx * r / 4d / Math.PI;

                double CTorq = omega * omega * r * r * r * r / 4d + vx * vx * r * r / 8d / Math.PI;

                //          float CTilt = omega * r * r * r * vx / PI + r * vx * vx * vx / omega;

                thrust = torque * ldr * CLift / CTorq;
                thrust *= flowMult * ispMult;
                //           tilt = lift / CLift * CTilt;

                fuelFlow = BSFC * power * flowMult;
                Isp = thrust / (fuelFlow * 9.80665d);
                SFC = 3600d / Isp;
            }
        }

        public double GetPower()
        {
            return power;
        }

        public double SASMultiplier()
        {
            return Math.Max(thrust / weight / 9.80665d, 0d);
        }

        public override bool GetRunning() { return combusting; }
    }
}
