using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{


    public class AJEModule : PartModule
    {
        public AJESolver aje;

        [KSPField(isPersistant = false, guiActive = false)]
        public float IspMultiplier = 1f;
        [KSPField(isPersistant = false, guiActive = false)]
        public int defaultentype = 0;

        [KSPField(isPersistant = false, guiActive = false)]
        public bool useOverheat = true;
        //       [KSPField(isPersistant = false, guiActive = true)]
        public float parttemp;
        //       [KSPField(isPersistant = false, guiActive = true)]
        public float maxtemp;


        [KSPField(isPersistant = false, guiActive = false)]
        public bool useMultiMode = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool isReactionEngine = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public float idle = 0.03f;
        [KSPField(isPersistant = false, guiActive = false)]
        public int abflag = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float acore = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float byprat = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float fhv = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float prat13 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float prat3 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float prat2 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float prat4 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float tinlt = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float tfan = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float tt7 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float tt4 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float tcomp = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta2 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta13 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta3 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta4 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta5 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float eta7 = -1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float maxThrust = 99999;

        [KSPField(isPersistant = false, guiActive = false)]
        public float ABIspMult = 1.0f;


        public EngineWrapper engine;
        public bool useAB = false;
        public float ABthreshold = 0.667f;
        public float ABmax, ABmin;

        [KSPField(isPersistant = false, guiActive = true)]
        public String Environment;

        [KSPField(isPersistant = false, guiActive = true)]
        public String Mode;
        [KSPField(isPersistant = false, guiActive = true)]
        public String Inlet;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public float Need_Area;
        //      [KSPField(isPersistant = false, guiActive = true)]
        public float fireflag;

        public float OverallThrottle = 0;


        // NK
        [KSPField(isPersistant = false, guiActive = false)]
        public FloatCurve prat3Curve = new FloatCurve();

        [KSPField(isPersistant = false, guiActive = false)]
        bool usePrat3Curve = false;


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("prat3Curve"))
            {
                //prat3Curve.Load(node.GetNode("prat3Curve"));
                print("AJE for part " + part.name + " found prat3Curve, usePrat3Curve = " + usePrat3Curve);
                float min, max;
                prat3Curve.FindMinMaxValue(out min, out max);
                print("curve: " + prat3Curve.minTime + ": " + min + "; " + prat3Curve.maxTime + ": " + max);
            }
        }


        public override void OnStart(StartState state)
        {

            if (state == StartState.Editor)
            {
                Need_Area = acore * (1 + byprat);
                return;
            }
            if (vessel == null)
                return;
            engine = new EngineWrapper(part);
            engine.useVelocityCurve = false;
            engine.IspMultiplier = IspMultiplier;
            engine.idle = idle;
            engine.ThrustUpperLimit = maxThrust;
            part.maxTemp = 3600f;
            engine.heatProduction = 360f;
            aje = new AJESolver();
            aje.setDefaults();

            switch (defaultentype)
            {
                case 1:
                    aje.loadJ85();
                    break;
                case 2:
                    aje.loadF100();
                    break;
                case 3:
                    aje.loadCF6();
                    break;
                case 4:
                    aje.loadRamj();
                    break;
            }
            if (true)
            {
                if (acore != -1 && byprat != -1)
                {
                    aje.areaCore = acore;
                    aje.byprat = byprat;
                    aje.areaFan = acore * (1.0 + byprat);
                    aje.a2d = aje.a2 = aje.areaFan;

                }
                if (tt4 != -1)
                    aje.tt[4] = aje.tt4 = aje.tt4d = tt4;
                if (prat3 != -1)
                    aje.prat[3] = aje.p3p2d = prat3;
                if (prat13 != -1)
                    aje.prat[13] = aje.p3fp2d = prat13;
                if (tcomp != -1)
                    aje.tcomp = tcomp;
                if (fhv != -1)
                    aje.fhvd = aje.fhv = fhv;
                if (tt7 != -1)
                    aje.tt[7] = aje.tt7 = aje.tt7d = tt7;
                if (eta2 != -1)
                    aje.eta[2] = eta2;
                if (prat2 != -1)
                    aje.prat[2] = prat2;
                if (prat4 != -1)
                    aje.prat[4] = prat4;
                if (eta3 != -1)
                    aje.eta[3] = eta3;
                if (eta4 != -1)
                    aje.eta[4] = eta4;
                if (eta5 != -1)
                    aje.eta[5] = eta5;
                if (eta7 != -1)
                    aje.eta[7] = eta7;
                if (eta13 != -1)
                    aje.eta[13] = eta13;
                if (tinlt != -1)
                    aje.tinlt = tinlt;
                if (tfan != -1)
                    aje.tfan = tfan;
                if (tcomp != -1)
                    aje.tcomp = tcomp;
                if (abflag != -1)
                    aje.abflag = abflag;
            }
            if (aje.abflag == 1 && (!isReactionEngine))
            {
                useAB = true;
                ABmax = (float)aje.tt7;
                ABmin = (float)(aje.tt4);
            }
            if (part.partInfo.partPrefab.Modules.Contains("AJEModule"))
            {
                AJEModule a = (AJEModule)part.partInfo.partPrefab.Modules["AJEModule"];
                usePrat3Curve = a.usePrat3Curve;
                prat3Curve = a.prat3Curve;
            }
            if (usePrat3Curve)
            {
                print("AJE OnStart for part " + part.name + " found prat3Curve");
                float min, max;
                prat3Curve.FindMinMaxValue(out min, out max);
                print("curve: " + prat3Curve.minTime + ": " + min + "; " + prat3Curve.maxTime + ": " + max);
                aje.fsmach = 0.0;
                aje.prat[3] = aje.p3p2d = prat3Curve.Evaluate(0.0f);
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

            #region Inlet

            float Arearatio, OverallTPR = 0, EngineArea = 0, InletArea = 0;
            AJEModule e;
            AJEInlet i;
            foreach (Part p in vessel.parts)
            {

                if (p.Modules.Contains("AJEModule"))
                {
                    e = (AJEModule)p.Modules["AJEModule"];
                    EngineArea += (float)(e.aje.areaCore * (1 + e.aje.byprat));
                }
                if (p.Modules.Contains("AJEInlet"))
                {
                    i = (AJEInlet)p.Modules["AJEInlet"];
                    if (((ModuleResourceIntake)p.Modules["ModuleResourceIntake"]).intakeEnabled)
                    {
                        InletArea += i.Area;
                        OverallTPR += i.Area * i.cosine * i.cosine * i.GetTPR((float)aje.fsmach);
                    }
                }
            }
            if (InletArea > 0)
                OverallTPR /= InletArea;
            Arearatio = Mathf.Min(InletArea / EngineArea, 1f);
            aje.eta[2] = Mathf.Clamp(Mathf.Sqrt(Arearatio) * OverallTPR, 0.001f, 1f);

            Inlet = "Area:" + ((int)(Arearatio * 100f)).ToString() + "%  TPR:" + ((int)(OverallTPR * 100f)).ToString() + "%";

            #endregion

            aje.FARps0 = FlightGlobals.getStaticPressure(vessel.altitude, vessel.mainBody);
            aje.FARts0 = FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.15f;

            Environment = (((int)(aje.FARps0 * 100f)) / 100f).ToString() + "atm;" + (((int)(aje.FARts0 * 100f)) / 100f) + "K";

            if (usePrat3Curve)
            {
                aje.prat[3] = aje.p3p2d = (double)(prat3Curve.Evaluate((float)aje.fsmach));
            }
            aje.u0d = part.vessel.srfSpeed * 3.6d;
            OverallThrottle = vessel.ctrlState.mainThrottle * engine.thrustPercentage / 100f;
            if (!useAB)
            {

                aje.comPute();
                engine.SetThrust(((float)aje.forceNetlb) * 0.004448f);
                engine.SetIsp((float)aje.isp);
                Mode = "Cruise " + System.Convert.ToString((int)(OverallThrottle * 100f)) + "%";
            }
            else
            {
                if (OverallThrottle <= ABthreshold)
                {
                    engine.useEngineResponseTime = true;
                    aje.abflag = 0;
                    aje.comPute();
                    engine.SetThrust(((float)aje.forceNetlb) * 0.004448f / ABthreshold);
                    engine.SetIsp((float)aje.isp);
                    Mode = "Cruise " + System.Convert.ToString((int)(OverallThrottle / ABthreshold * 100f)) + "%";
                }
                else
                {
                    // only allow instance response when already at max RPM; if the compressor is still spooling up, it's still spooling up.
                    if(engine.currentThrottle > ABthreshold)
                        engine.useEngineResponseTime = false;
                    else
                        engine.useEngineResponseTime = true;

                    aje.abflag = 1;
                    aje.tt7 = (OverallThrottle - ABthreshold) * (ABmax - ABmin) / (1 - ABthreshold) + ABmin;

                    aje.comPute();
                    engine.SetThrust(((float)aje.forceNetlb) * 0.004448f / OverallThrottle);
                    engine.SetIsp((float)aje.isp * ABIspMult);
                    Mode = "Afterburner " + System.Convert.ToString((int)((OverallThrottle - ABthreshold) / (1 - ABthreshold) * 100f)) + "%";
                }

            }
            Mode += " (" + (aje.forceGrosslb * 0.004448f).ToString("N2") + "kN gr)";




            if (aje.fireflag > 0.9f && useOverheat)
            {
                part.temperature = (aje.fireflag * 2f - 1.2f) * part.maxTemp;

            }

            fireflag = aje.fireflag;
            parttemp = part.temperature;
            maxtemp = part.maxTemp;


            //           mach = (float)aje.fsmach;


        }


    }


}

