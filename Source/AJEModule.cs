using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{


    public class AJEModule : PartModule
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

        public AJESolver aje;
        public EngineWrapper engine;
        float acturalThrottle = 0;
        public List<AJEModule> engineList;
        public List<AJEInlet> inletList;
        public float OverallTPR = 1, Arearatio = 1;
        public void Start()
        {
            Need_Area = Area * (1 + BPR);
            acturalThrottle = 0;
            engine = new EngineWrapper(part);
            engine.idle = 1f;
            engine.IspMultiplier = 1f;
            engine.useVelocityCurve = false;
            engine.ThrustUpperLimit = maxThrust;
            part.maxTemp = 1800;
            if (TIT > 0)
                part.maxTemp = TIT;
            engine.heatProduction = part.maxTemp * 0.1f;
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
                engine.engineDecelerationSpeed = .1f / (Area * (1 + BPR));
                engine.engineAccelerationSpeed = .1f / (Area * (1 + BPR));
            }
            else
            {           //It's not like there's anything in a ramjet to spool, now is there?
                engine.useEngineResponseTime = false;
            }
            engineList.Clear();
            inletList.Clear();
            for (int j = 0; j < vessel.parts.Count; j++)        //reduces garbage produced compared to foreach due to Unity Mono issues
            {
                Part p = vessel.parts[j];
                if (p.Modules.Contains("AJEModule"))
                {
                    engineList.Add((AJEModule)p.Modules["AJEModule"]);          //consider storing list of affected AJEModules and AJEInlets, perhaps a single one for each vessel.  Would result in better performance     
                }
                if (p.Modules.Contains("AJEInlet"))
                {
                    inletList.Add((AJEInlet)p.Modules["AJEInlet"]); 
                }
            }
        }
              
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (engine.type == EngineWrapper.EngineType.NONE || !engine.EngineIgnited)
                return;
            if (vessel.mainBody.atmosphereContainsOxygen == false || part.vessel.altitude > vessel.mainBody.maxAtmosphereAltitude)
            {
                engine.SetThrust(0);
                return;
            }

            UpdateInletEffects();
            UpdateFlightCondition(vessel.altitude, part.vessel.srfSpeed, vessel.mainBody);

            if(CPR == 1 && aje.GetM0()<0.3)//ramjet
            {
                engine.SetThrust(0);
                engine.SetIsp(1000);
            }
            else
            {
                engine.SetThrust((float)aje.GetThrust() / 1000f / Arearatio);
                engine.SetIsp((float)aje.GetIsp());
            }
            float fireflag = (float)aje.GetT3()/maxT3;
            if (fireflag > 0.9f )
            {
                part.temperature = (fireflag * 2f - 1.2f) * part.maxTemp;
            }
        }

        //ferram4: separate out so function can be called separately for editor sims
        public void UpdateInletEffects()
        {
            float EngineArea = 0, InletArea = 0;
            OverallTPR = 0;

            if (aje == null)
                Debug.Log("HOW?!");
            float M0 = (float)aje.GetM0();
            
            for (int j = 0; j < engineList.Count; j++)       
            {
                AJEModule e = engineList[j];
                if(e)
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
                    OverallTPR += i.Area * i.cosine * i.cosine * i.GetTPR(M0);
                }
            }
        
            if (InletArea > 0)
                OverallTPR /= InletArea;
            Arearatio = Math.Min(InletArea / EngineArea, 1f);
            Inlet = "Area:" + Arearatio.ToString("P2") + " TPR:" + OverallTPR.ToString("P2");

        }

        public void UpdateFlightCondition(double altitude, double vel, CelestialBody body)
        {
            double p0 = FlightGlobals.getStaticPressure(altitude, body);
            double t0 = FlightGlobals.getExternalTemperature((float)altitude, body) + 273.15;

            Environment = p0.ToString("N2") + " Atm;" + t0.ToString("N2") + " K ";
            if (CPR != 1)
            {     
                float requiredThrottle = (int)(vessel.ctrlState.mainThrottle * engine.thrustPercentage); //0-100
                float deltaT = (float)TimeWarp.fixedDeltaTime;
                float throttleResponseRate = Mathf.Max(2 / Area / (1 + BPR), 5); //percent per second

                float d = requiredThrottle - acturalThrottle;
                if (Mathf.Abs(d) > throttleResponseRate * deltaT)
                    acturalThrottle += Mathf.Sign(d) * throttleResponseRate * deltaT;
            }
            else // ramjet
            {
                acturalThrottle = (int)(vessel.ctrlState.mainThrottle * engine.thrustPercentage);
            }

            aje.SetTPR(OverallTPR);
            aje.CalculatePerformance(p0 * 101.3, t0, vel, (acturalThrottle + 1) / 100);
            
        }


    }
}

