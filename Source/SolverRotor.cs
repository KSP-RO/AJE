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
        private float omega0, r, weight, power0, rho0;//0 stands for max-takeoff condition
        private float buff, BSFC;
        
        public float power, shaftpower, outputpower;

        private bool useOxygen = true;

        private bool combusting = false;
        Vector3 t, f;//thrust, forward normal vector
        Vector3 wind;//wind
        public float omega, inertia;//variable angular speed, angular inertia
        float a; float CL0, AoA0, LDR0, CD0;//AoA, max lift coefficient, AoA of max takeoff
        
        public Vector3 Force, Torque, Drag, Tilt;//in Newton,meter
        float mach1, dihedral;
        int clockWise;
        SmoothInput smoothinputX, smoothinputY, smoothinputZ, smoothinputP;
        public SolverRotor(float omega, float r, float weight, float power0, float rho0, float buff, float BSFC, bool useOxygen, int clockWise)
        {
            this.omega0 = omega;
            this.r = r;
            this.weight = weight;
            this.power0 = power0;
            this.rho0 = rho0;
            this.buff = buff;
            this.BSFC = BSFC;
            this.useOxygen = useOxygen;
            this.clockWise = clockWise;

            AoA0 = 20f;
            CL0 = 6 * 9.80665f * weight / rho0 / omega0 / omega0 / r / r;
            LDR0 = 3f * r * 9.80665f * weight * omega0 / 4f / power0;
            CD0 = CL0 / LDR0;
            inertia = weight / 15 / 3 * r * r * r; //assume the rotor is 1/15 of max take-off weight
            this.omega = 1;
            this.power = 0;
            this.dihedral = 0;
            float tau = 30 / omega0;
            smoothinputX = new SmoothInput(tau/2);
            smoothinputY = new SmoothInput(tau/2);
            smoothinputZ = new SmoothInput(tau);
            smoothinputP = new SmoothInput(1e-6f);
        }

        float LiftCoefficient(float x)
        {
            float y;
            x = x / AoA0;
           // if (x < 1)
                y = x;
         //   else if (x < 1.5)
         //       y = 1 - 4 * (x - 1) * (x - 1);
         //   else
         //       y = 0;
            return CL0 * y;
        }

        float DragCoefficient(float a, float v)
        {
            float y = 1.25e-3f * a * a + 0.5f;
            v = v / mach1; //convert speed to mach number
            if (v > 0.8)
                y += y * (v - 0.8f) * (v - 0.8f) * 50f;
            return CD0 * y;
        }

        Vector3 choppercontrol;
        Vector3 v;//air speed
        float radarAltitude, thrustlimiter;

        public void UpdateFlightParams(Vector3 controlInput, Vector3 vel,Vector3 forward,Vector3 thrust,float radar,float speedofsound,float thrustlimiter)
        {
            this.choppercontrol = controlInput;
          /*  choppercontrol.x = smoothinputX.Smooth(choppercontrol.x);
            choppercontrol.y = smoothinputY.Smooth(choppercontrol.y);
            choppercontrol.z = smoothinputZ.Smooth(choppercontrol.z);
            */

            float r = choppercontrol.x * choppercontrol.x + choppercontrol.y * choppercontrol.y;
            if (r > 1)
            {
                r = Mathf.Sqrt(r);
                choppercontrol.x = choppercontrol.x / r;
                choppercontrol.y = choppercontrol.y / r;
            }
            choppercontrol.z = Mathf.Clamp01(choppercontrol.z);
            this.v = vel;
            this.f = forward;
            this.t = thrust;
            this.radarAltitude = radar;
            this.mach1 = speedofsound;
            this.thrustlimiter = thrustlimiter;
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
                power = smoothinputP.Smooth(power0 * (float)Math.Min(1d, rho / rho0) * thrustlimiter / 100);
                
                Force = Tilt = Torque = Drag = Vector3.zero;
                wind = -v;

                Vector3 fxt = Vector3.Cross(f, this.t);
                fxt.Normalize();
                
                Vector3 b = Quaternion.AngleAxis(-dihedral, fxt) * (Quaternion.AngleAxis(90, this.t) * fxt);//parallel to blade,5deg dihedral

            
                if (clockWise == -1 || clockWise == 0)
                {
                    for (int i = 0; i < 24; i++)//each blade
                    {
                        Vector3 txb = Vector3.Cross(this.t, b);
                        txb.Normalize();
                        Vector3 txbxb = Vector3.Cross(txb, b);

                        float vz = Vector3.Dot(wind, t);//wind speed across the disc
                        a = AoA0 * choppercontrol.z;//collective
                        if (vz > 0 && buff > 1)
                        {
                            a *= 1 + (buff - 1) / 100 * vz;
                        }
                        a -= AoA0 * choppercontrol.z * Mathf.Sin(Mathf.PI / 12 * i) * choppercontrol.y;//cyclic done by swash plate
                        a += AoA0 * choppercontrol.z * Mathf.Cos(Mathf.PI / 12 * i) * choppercontrol.x;

                        Vector3 e = Quaternion.AngleAxis(a, b) * txbxb;
                        //float areaCoefficient = 1 - vx / 200; //a fake flapping hinge + drag hinge

                        Vector3 bladeForce = Vector3.zero;
                        Vector3 bladeTorque = Vector3.zero;

                        for (float j = 0.5f; j < 12; j++)//each segment of a blade
                        {
                            float x = r / 12 * j;
                            Vector3 flow = txb * (omega * x);
                            flow = flow + wind;//air flow = wind + blade element speed           
                            float AoA = 90 - Vector3.Angle(e, flow);
                            //air flow through the disc results in AoA increase
                            
                            float CL = LiftCoefficient(AoA);
                            float CD = DragCoefficient(AoA, flow.magnitude);

                            Vector3 lift = rho0 / 2 * CL / 288 * Vector3.Dot(flow, flow) * Vector3.Cross(flow, Vector3.Cross(e, flow)).normalized;
                            Vector3 drag = rho0 / 2 * CD / 288 * Vector3.Dot(flow, flow) * flow.normalized;
                            Vector3 force = lift + drag;
                        
                            bladeForce += force;
                            bladeTorque += Vector3.Cross(b, force) * x;
                        }
                        Force += bladeForce;
                        Torque += bladeTorque;

                        b = Quaternion.AngleAxis(15, this.t) * b;//rotate blade 15 deg
                    }//end of blade calculation
                    Tilt += Quaternion.AngleAxis(-90, t) * Vector3.ProjectOnPlane(Torque, t);// tilt lags 90 degrees because of gyroscopic precession

                }
                if (clockWise == 0)
                    shaftpower = Vector3.Dot(Torque, t) * omega;
                if (clockWise == 1 || clockWise == 0)
                {
                    for (int i = 0; i < 24; i++)//each blade
                    {
                        Vector3 txb = -Vector3.Cross(this.t, b);
                        txb.Normalize();
                        Vector3 txbxb = Vector3.Cross(txb, b);

                        float vz = Vector3.Dot(wind, t);//wind speed across the disc
                        a = AoA0 * choppercontrol.z;//collective
                        if (vz > 0 && buff > 1)
                        {
                            a *= 1 + (buff - 1) / 100 * vz;
                        }
                        a += AoA0 * choppercontrol.z * Mathf.Sin(Mathf.PI / 12 * i) * choppercontrol.y;//cyclic done by swash plate
                        a -= AoA0 * choppercontrol.z * Mathf.Cos(Mathf.PI / 12 * i) * choppercontrol.x;

                        Vector3 e = Quaternion.AngleAxis(-a, b) * txbxb;
                        //float areaCoefficient = 1 - vx / 200; //a fake flapping hinge + drag hinge

                        Vector3 bladeForce = Vector3.zero;
                        Vector3 bladeTorque = Vector3.zero;

                        for (float j = 0.5f; j < 12; j++)//each segment of a blade
                        {
                            float x = r / 12 * j;
                            Vector3 flow = txb * (omega * x);
                            flow = flow + wind;//air flow = wind + blade element speed           
                            float AoA = 90 - Vector3.Angle(e, flow);
                            //air flow through the disc results in AoA increase
                            
                            float CL = LiftCoefficient(AoA);
                            float CD = DragCoefficient(AoA, flow.magnitude);

                            Vector3 lift = rho0 / 2 * CL / 288 * Vector3.Dot(flow, flow) * Vector3.Cross(flow, Vector3.Cross(e, flow)).normalized;
                            Vector3 drag = rho0 / 2 * CD / 288 * Vector3.Dot(flow, flow) * flow.normalized;
                            Vector3 force = lift + drag;

                            bladeForce += force;
                            bladeTorque += Vector3.Cross(b, force) * x;
                        }
                        Force += bladeForce;
                        Torque += bladeTorque;

                        b = Quaternion.AngleAxis(15, this.t) * b;//rotate blade 15 deg
                    }//end of blade calculation
                    Tilt += Quaternion.AngleAxis(90, t) * Vector3.ProjectOnPlane(Torque, t);// tilt lags 90 degrees because of gyroscopic precession

                }

                Force = Quaternion.AngleAxis(Tilt.magnitude / weight, Tilt) * Force; // flapping hinge tilts the disc 

                if (clockWise == 0)
                {
                    Force = Force / 2f;
                    Drag = Drag / 2f;
                    Tilt = Tilt / 2f;
                }

                Torque = Vector3.Project(Torque, t); //torque on shaft
                if (clockWise == 1)
                    shaftpower = -Vector3.Dot(Torque, t) * omega;
                if (clockWise == -1)
                    shaftpower = Vector3.Dot(Torque, t) * omega;


                if (omega <= omega0)
                {
                    omega += (power - shaftpower) / omega / inertia * Time.deltaTime;
                }
                else
                {
                    omega = omega0;                    
                }

                thrust = -Vector3.Dot(Force,t) * flowMult * ispMult;
                //effective translational lift
                float crossWind = Vector3.ProjectOnPlane(v,t).magnitude;
                if (crossWind > 10)
                    thrust *= 1.2f;
                else if (crossWind > 5)
                    thrust *= 0.2 / 5 * (crossWind - 5) + 1;
                //happens at 10-20kn

                fuelFlow = BSFC * power * flowMult;
                if ((omega0 - omega) < 1)
                {
                    fuelFlow *= Mathf.Clamp01(shaftpower / power);
                }

                outputpower = Mathf.Min(shaftpower, power);
                               
                float groundEffect, radarOverR;
                radarOverR = radarAltitude / r;
                if (radarOverR<0.5f)
                {
                    groundEffect = 0.07f;
                } else if(radarOverR<1.5f)
                {
                    groundEffect = 0.07f * (radarOverR - 1.5f) * (radarOverR - 1.5f);
                } else
                {
                    groundEffect = 0;
                }
                if (groundEffect>0)
                {
                    thrust *= 1 + groundEffect;
                }

                dihedral = 5 * (float)thrust / (weight * 9.80665f);

                Isp = thrust / (fuelFlow * 9.80665d);
                SFC = 3600d / Isp;

            }
      
        }

        public double GetPower()
        {
            return shaftpower;
        }

  

        public override bool GetRunning() { return combusting; }
    }
}
