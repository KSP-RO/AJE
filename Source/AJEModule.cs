using KSP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace AJE
{


    public class ModuleEnginesAJEJet : ModuleEnginesFX
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float Area = 0.1f;
        public float TPR = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float BPR = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float CPR = 20;
        [KSPField(isPersistant = false, guiActive = false)]
        public float FPR = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float Mdes = 0.9f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float Tdes = 250;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_c = 0.95f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_t = 0.98f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta_n = 0.9f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float FHV = 46.8E6f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TIT = 1200;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TAB = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool exhaustMixer = false;
        [KSPField(isPersistant = false, guiActive = true)]
        public String Inlet;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public float Need_Area;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxThrust = 999999;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxT3 = 9999;
        [KSPField(isPersistant = false, guiActive = true)]
        public String Environment;
        [KSPField(isPersistant = true, guiActive = false)]
        public float actualThrottle = 0;

        // engine temp stuff
        [KSPField(isPersistant = false, guiActive = true, guiName = "Engine Temp", guiUnits = " K")]
        public float engineTemp = 0f;
        protected VInfoBox overheatBox = null;

        // overrides from ModuleEngines
        [KSPField(guiActive = true, guiName = "Fuel Flow", guiUnits = "kg/sec", guiFormat = "F5")]
        new public float fuelFlowGui;

        public AJESolver aje;
        public List<ModuleEnginesAJEJet> engineList;
        public List<AJEInlet> inletList;
        public double OverallTPR = 1, Arearatio = 1;

        private const double invg0 = 1d / 9.80665d;
        public void Start()
        {
            Need_Area = Area * (1 + BPR);
            
            ThrustUpperLimit = maxThrust;
 //           bool DREactive = AssemblyLoader.loadedAssemblies.Any(
 //               a => a.assembly.GetName().Name.Equals("DeadlyReentry.dll", StringComparison.InvariantCultureIgnoreCase));
            if (TIT > part.maxTemp)
                part.maxTemp = TIT;
   //         heatProduction = (float)part.maxTemp * 0.1f;
            aje = new AJESolver();
            aje.InitializeOverallEngineData(
                Area,
                TPR,
                BPR,
                CPR,
                FPR,
                Mdes,
                Tdes,
                eta_c,
                eta_t,
                eta_n,
                FHV,
                TIT,
                TAB,
                exhaustMixer
                );

            if (CPR != 1)
            {
                engineDecelerationSpeed = .1f / (Area * (1 + BPR));
                engineAccelerationSpeed = .1f / (Area * (1 + BPR));
            }
            else
            {           //It's not like there's anything in a ramjet to spool, now is there?
                useEngineResponseTime = false;
            }
            engineList.Clear();
            inletList.Clear();
            for (int j = 0; j < vessel.parts.Count; j++)        //reduces garbage produced compared to foreach due to Unity Mono issues
            {
                Part p = vessel.parts[j];
                if (p.Modules.Contains("AJEModule"))
                {
                    engineList.Add((ModuleEnginesAJEJet)p.Modules["AJEModule"]);          //consider storing list of affected AJEModules and AJEInlets, perhaps a single one for each vessel.  Would result in better performance     
                }
                if (p.Modules.Contains("AJEInlet"))
                {
                    inletList.Add((AJEInlet)p.Modules["AJEInlet"]); 
                }
            }

            // set initial params
            engineTemp = 288.15f;
        }
              
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor || TimeWarping())
            {
                realIsp = 1f;
                finalThrust = 0f;
                return;
            }
            if (EngineIgnited)
            {
                UpdatePropellantStatus();
                if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude <= vessel.mainBody.atmosphereDepth)
                {
                    UpdateInletEffects();
                    UpdateFlightCondition(vessel.altitude, part.vessel.srfSpeed, vessel.mainBody);
                    double thrustOut = 0d;
                    if (CPR == 1 && aje.GetM0() < 0.3)//ramjet
                    {
                        realIsp = 1f;
                    }
                    else
                    {
                        thrustOut = CalculateThrustParams();
                    }
                    finalThrust = (float)thrustOut * vessel.VesselValues.EnginePower.value;

                    // Heat
                    double fireflag = aje.GetT3() / maxT3;
                    if (fireflag >= 0.79999d)
                    {
                        if (fireflag > 1d)
                        {
                            FlightLogger.eventLog.Add("[" + FormatTime(vessel.missionTime) + "] " + part.partInfo.title + " melted its internals from airstream heat.");
                            part.explode();
                        }
                        if (overheatBox == null)
                        {
                            overheatBox = part.stackIcon.DisplayInfo();
                            overheatBox.SetMsgBgColor(XKCDColors.DarkRed.A(0.6f));
                            overheatBox.SetMsgTextColor(XKCDColors.OrangeYellow.A(0.6f));
                            overheatBox.SetMessage("Engine overheat!");
                            overheatBox.SetProgressBarBgColor(XKCDColors.DarkRed.A(0.6f));
                            overheatBox.SetProgressBarColor(XKCDColors.OrangeYellow.A(0.6f));
                        }
                        overheatBox.SetValue((float)fireflag * 2f - 1f, 0.6f, 1.0f);
                    }
                    else
                    {
                        overheatBox = null;
                    }
                }
            }
        }

        //ferram4: separate out so function can be called separately for editor sims
        public void UpdateInletEffects()
        {
            double EngineArea = 0, InletArea = 0;
            OverallTPR = 0;

            if (aje == null)
                Debug.Log("HOW?!");
            double M0 = aje.GetM0();
            int eCount = engineList.Count;
            for (int j = 0; j < eCount; ++j)
            {
                ModuleEnginesAJEJet e = engineList[j];
                if((object)e != null)
                {
                    EngineArea += e.Area * (1 + e.BPR);
                }
            }
                        
            for (int j = 0; j < inletList.Count; j++)        
            {
                AJEInlet i = inletList[j];
                if(i)
                {
                    InletArea += i.Area;
                    OverallTPR += i.Area * i.cosine * i.cosine * i.GetTPR((float)M0);
                }
            }
        
            if (InletArea > 0)
                OverallTPR /= InletArea;
            Arearatio = Math.Min(InletArea / EngineArea, 1f);
            Inlet = "Area:" + Arearatio.ToString("P2") + " TPR:" + OverallTPR.ToString("P2");

        }

        public void UpdateFlightCondition(double altitude, double vel, CelestialBody body)
        {
            double p0 = vessel.staticPressurekPa;//in Kpa
            double t0 = vessel.externalTemperature; //in Kelvin
            Environment = p0.ToString("N2") + " kPa;" + t0.ToString("N2") + " K ";

            if (CPR != 1)
            {     
                float requiredThrottle = (int)(vessel.ctrlState.mainThrottle * thrustPercentage); //0-100
                float deltaT = (float)TimeWarp.fixedDeltaTime;
                float throttleResponseRate = Mathf.Max(2 / Area / (1 + BPR), 5); //percent per second

                float d = requiredThrottle - actualThrottle;
                if (Mathf.Abs(d) > throttleResponseRate * deltaT)
                    actualThrottle += Mathf.Sign(d) * throttleResponseRate * deltaT;
                else
                    actualThrottle = requiredThrottle;
            }
            else // ramjet
            {
                actualThrottle = (int)(vessel.ctrlState.mainThrottle * thrustPercentage);
            }

            aje.SetTPR(OverallTPR);
            aje.CalculatePerformance(p0, t0, vel, (actualThrottle + 1) / 100);
            
        }

        public double CalculateThrustParams()
        {
            double thrustIn = aje.GetThrust() * Arearatio; //in N
            double isp = aje.GetIsp();
            double producedThrust = 0d;
            double massFlow = 0d;
            double propellantRecieved = 0d;

            double vesselValue = vessel.VesselValues.FuelUsage.value;
            if (vesselValue == 0d)
                vesselValue = 1d;

            double fuelFlow = thrustIn * invg0 / isp * vesselValue;
            massFlow = fuelFlow * 0.001d * TimeWarp.fixedDeltaTime; // in tons

            if (thrustIn <= 0d)
            {
                Flameout("Air combustion failed");
                propellantRecieved = 0d;
            }
            else
            {
                if (CheatOptions.InfiniteFuel == true)
                {
                    propellantRecieved = 1d;
                    UnFlameout();
                }
                else
                {
                    propellantRecieved = RequestPropellant(massFlow);
                }
            }

            producedThrust = thrustIn * propellantRecieved;
            fuelFlowGui = (float)(fuelFlow * propellantRecieved * vesselValue);
            return producedThrust * 0.001d; // to kN
        }

        static string FormatTime(double time)
        {
            int iTime = (int)time % 3600;
            int seconds = iTime % 60;
            int minutes = (iTime / 60) % 60;
            int hours = (iTime / 3600);
            return hours.ToString("D2")
                + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
        }
    }
}

