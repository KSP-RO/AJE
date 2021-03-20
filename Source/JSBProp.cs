using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using System.Reflection;

namespace AJE
{
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
            nRows = (uint)node.values.Count - 1;
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
                        Data[i, j + offset] = dtmp;
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
                    curRow += Data[i, j] + " ";
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
            else*/
            if (rFactor < 0.0) rFactor = 0.0;

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
        const double FTTOM = 0.3048d;
        const double INTOM = 0.0254d;
        const double FTLBTOJ = 1.3558179491d; // W/HP divided by ft-lb/HP

        public void LogVars()
        {
            Debug.Log("Dumping propeller status");
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
            RPM = 0d;
            if (node != null)
                Load(node);
            CalcDefaults();
        }
        public AJEPropJSB(string propName, double minR = -1, double maxR = -1, double diam = -1, double ixx = -1)
        {
            SetDefaults();


            RPM = 0d;
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
            P_Factor = t.P_Factor;
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

        public double GetMinRPM() { return MinRPM; }
        public double GetMaxRPM() { return MaxRPM; }

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
            double RPS = RPM * (1d / 60d);

            if (Math.Abs(RPS) > 0.001)
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
                            if (RPM > 200d) Pitch -= dRPM * deltaT;
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
                            if (RPM > 200d) Pitch += (PitchReq - Pitch) * (1d / 200d);
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
                        Pitch += (MaxPitch - Pitch) * (1d / 300d); // just a guess (about 5 sec to fully feathered)
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

            double local_RPS = RPS < 0.01d ? 0.01d : RPS;

            PowerRequired = cPReq * local_RPS * local_RPS * local_RPS * D5 * rho;
            vTorque.x = (-Sense * PowerRequired / (local_RPS * 2.0 * Math.PI));

            //Debug.Log("Cp = " + cPReq);

            return PowerRequired;
        }

        /** Calculates and returns the thrust produced by this propeller.
            Given the excess power available from the engine (in watts), the thrust is
            calculated, as well as the current RPM. The RPM is calculated by integrating
            the torque provided by the engine over what the propeller "absorbs"
            (essentially the "drag" of the propeller).
            @param PowerAvailable this is the excess power provided by the engine to
            accelerate the prop. It could be negative, dictating that the propeller
            would be slowed.
            @return the thrust in newtons */
        public double Calculate(double EnginePower, double rho, double Vel, double speedOfSound, double deltaTime)
        {
            deltaT = deltaTime;
            double omega, PowerAvailable;
            double RPS = RPM * (1d/ 60d);
            double machInv = 1d / speedOfSound;
            // Calculate helical tip Mach
            double Area = 0.25d * Diameter * Diameter * Math.PI;
            double Vtip = RPS * Diameter * Math.PI;
            HelicalTipMach = Math.Sqrt(Vtip * Vtip + Vel * Vel) * machInv;

            PowerAvailable = EnginePower - GetPowerRequired(rho, Vel);

            if (RPS > 0d)
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
            double CtMachFactor = 1;
            if (CtMach != null)
            {
                CtMachFactor = Math.Pow(CtMach.Evaluate((float)HelicalTipMach), MachPowTweak);
                ThrustCoeff *= CtMachFactor;

            }

            Thrust = ThrustCoeff * RPS * RPS * D4 * rho;

            //Debug.Log("CT = " + ThrustCoeff + " CtFactor = " + CtFactor + "\n\rCtTweak = " + CtTweak + " CtMachFactor = " + CtMachFactor + "\n\rJ = " + J + "\n\rHelicalTipMach = " + HelicalTipMach + " SoundSpeed = " + speedOfSound);
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

            omega = RPS * 2d * Math.PI;

            // The Ixx value and rotation speed given below are for rotation about the
            // natural axis of the engine. The transform takes place in the base class
            // FGForce::GetBodyForces() function.

            /*vH(eX) = Ixx*omega*Sense;
            vH(eY) = 0.0;
            vH(eZ) = 0.0;*/

            if (omega > 0d)
                ExcessTorque = PowerAvailable / omega;
            else
                ExcessTorque = PowerAvailable;

            RPM = (RPS + (ExcessTorque / (Ixx * 2.0 * Math.PI) * deltaT)) * 60d;

            if (RPM < 0d) RPM = 0d; // Engine won't turn backwards

            // Transform Torque and momentum first, as PQR is used in this
            // equation and cannot be transformed itself.
            //vMn = in.PQR*(Transform()*vH) + Transform()*vTorque;

            // hacky mach drag -- should not be needed with nuFAR
            /*if (MachDrag != null)
            {
                double machDrag = MachDrag.Evaluate((float)(Vel * machInv));
                machDrag *= machDrag * D4 * RPS * RPS * rho * 0.00004d;
                Thrust -= machDrag;
            }*/

            //MonoBehaviour.print("Prop running: thrust " + Thrust + ", Ct " + ThrustCoeff + ", RPM " + RPM + ", PAvail " + PowerAvailable + ", J " + J + ", delta RPM " + (RPM - RPS * 60d));

            return Thrust;
        }
    }
}
