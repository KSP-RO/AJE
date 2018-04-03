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
        
        public float power, shaftpower;

        private bool useOxygen = true;

        private bool combusting = false;
        Vector3 t, f;//thrust, forward normal vector
        Vector3 wind;//wind
        public float omega, inertia;//variable angular speed, angular inertia
        float a; float CL0, AoA0, LDR0, CD0;//AoA, max lift coefficient, AoA of max takeoff
        public float Lift;
        public Vector3 Torque, Drag, Tilt;//in Newton,meter

        public SolverRotor(float omega, float r, float weight, float power0, float rho0, float buff, float BSFC, bool useOxygen=true)
        {
            this.omega0 = omega;
            this.r = r;
            this.weight = weight;
            this.power0 = power0;
            this.rho0 = rho0;
            this.buff = buff;
            this.BSFC = BSFC;
            this.useOxygen = useOxygen;

            AoA0 = 10f;
            CL0 = 6 * 9.80665f * weight / rho0 / omega0 / omega0 / r / r;
            LDR0 = 3f * r * 9.80665f * weight * omega0 / 4f / power0;
            CD0 = CL0 / LDR0;
            inertia = weight / 10 / 3 * r * r * r; //assume the rotor is 10% of max take-off weight
            this.omega = omega0/4;
        }
        float LiftCoefficient(float x)
        {
            float y;
            if (x < 15)
            {
                y = x / 10;
            }
            else if (x < 23.38)
            {
                float a0 = -4.53879310344824f;
                float a1 = 0.778448275862065f;
                float a2 = -0.0249999999999999f;
                y = a0 + a1 * x + a2 * x * x;
            }
            else
            {
                y = 0;
            }
            return CL0 * y;
        }
        float DragCoefficient(float a)
        {
            float y = 5e-3f * a * a + 0.5f;
            return CD0 * y;
        }
        Vector3 choppercontrol;
        Vector3 v;//air speed
        float radaraltitude;
        public void UpdateFlightParams(Vector3 choppercontrol, Vector3 vel,Vector3 forward,Vector3 thrust,float radar)
        {
            this.choppercontrol = choppercontrol;
            float r = choppercontrol.x * choppercontrol.x + choppercontrol.y * choppercontrol.y;
            if (r > 1)
            {
                r = Mathf.Sqrt(r);
                choppercontrol.x = choppercontrol.x / r;
                choppercontrol.y = choppercontrol.y / r;
            }
            this.v = vel;
            this.f = forward;
            this.t = thrust;
            this.radaraltitude = radar;
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
                power = power0 * (float)Math.Min(1d, rho / rho0);
                Lift = 0;
                Torque = Drag = Vector3.zero;
                wind = -v;

                Vector3 fxt = Vector3.Cross(f, this.t);
                fxt.Normalize();

                a = AoA0 * choppercontrol.z * 1.2f;//collective

                fxt = Vector3.Cross(f, this.t);
                fxt.Normalize();
                Vector3 b = Quaternion.AngleAxis(-2.5f, fxt) * (Quaternion.AngleAxis(90, this.t) * fxt);//parallel to blade,5deg dihedral

                for (int i = 0; i < 24; i++)//each blade
                {

                    Vector3 txb = Vector3.Cross(this.t, b);
                    txb.Normalize();
                    Vector3 txbxb = Vector3.Cross(txb, b);

                    float vx = Vector3.Dot(wind, txb);//wind speed across the blade
                                            
                    Vector3 e = Quaternion.AngleAxis(a, b) * txbxb;

                    float bladeLift = 0f;
                    Vector3 bladeTorque = Vector3.zero;
                    Vector3 bladeDrag = Vector3.zero;
            
                    for (float j = 0.5f; j < 12; j++)//each segment of a blade
                    {
                        float x = r / 12 * j;
                        Vector3 flow = txb * (omega * x);
                        flow = flow + wind;//air flow = wind + blade element speed           
                        float AoA = 90 - Vector3.Angle(e, flow);
                        //air flow through the disc results in AoA increase
                        //float CL = Mathf.Max(CL0 / AoA0 * AoA, 0);
                        float CL = LiftCoefficient(AoA);
                        float CD = DragCoefficient(AoA);


                        Vector3 lift = rho0 / 2 * CL / 288 * Vector3.Dot(flow, flow) * Vector3.Cross(flow, Vector3.Cross(e, flow)).normalized;
                        Vector3 drag = rho0 / 2 * CD / 288 * Vector3.Dot(flow, flow) * flow.normalized;
                        Vector3 force = lift + drag;
                        Vector3 l = Vector3.Dot(force, t) * t;

                        bladeLift += l.magnitude;
                        bladeDrag += (force - l);
                        bladeTorque += Vector3.Cross(b, force) * x;
                        

                    }
                    Lift += bladeLift;
                    Torque += bladeTorque;
                    Drag += bladeDrag;

                    b = Quaternion.AngleAxis(15, this.t) * b;//rotate blade 15 deg
                }//end of blade calculation

                Tilt = Vector3.ProjectOnPlane(Torque, t); // tilts the disc
                Torque = Vector3.Project(Torque, t); //torque on shaft
                shaftpower = Torque.magnitude * omega;


                if (omega <= omega0)
                {
                    omega += (power - shaftpower) / omega / inertia * Time.deltaTime;
                }
                else
                {
                    omega = omega0;
                }

                thrust = Lift * flowMult * ispMult;

                fuelFlow = BSFC * power * flowMult;
                fuelFlow *= (omega0 - omega) < 1 ? (Mathf.Clamp01(shaftpower/power)) : 1;

                float groudeffect, radartor;
                radartor = radaraltitude / r;
                if (radartor<0.5f)
                {
                    groudeffect = 0.07f;
                }else if(radartor<1.5f)
                {
                    groudeffect = 0.07f * (radartor - 1.5f) * (radartor - 1.5f);
                } else
                {
                    groudeffect = 0;
                }
                if (groudeffect>0)
                {
                    thrust *= 1 + groudeffect;
                }
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
