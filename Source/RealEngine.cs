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

    /// <summary>
    /// Base module for AJE engines
    /// Derive from this for a real engine; this *will not work* alone.
    /// </summary>
    public class ModuleRealEngine : ModuleEnginesFX, IModuleInfo
    {
        // base fields
        [KSPField(isPersistant = false, guiActive = true)]
        public String Environment;

        [KSPField(isPersistant = false, guiActive = true)]
        public String Inlet;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double Need_Area;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Throttle", guiUnits = "%")]
        public int actualThrottle;

        [KSPField(isPersistant = false)]
        public double thrustUpperLimit = double.MaxValue;

        // engine temp stuff
        // fields
        [KSPField(isPersistant = false)]
        public double maxEngineTemp;
        [KSPField(isPersistant = false, guiActive = true)]
        public string engineTempString;
        // internals
        protected double fireflag = 0d, engineTemp = 288.15d;
        protected VInfoBox overheatBox = null;


        // protected internals
        protected EngineSolver engineSolver = null;
        protected List<ModuleRealEngine> engineList;
        protected List<AJEInlet> inletList;
        protected double OverallTPR = 1d, Arearatio = 1d;

        protected Vector3 thrustOffset = Vector3.zero;
        protected Quaternion thrustRot = Quaternion.identity;

        protected int partsCount = 0;

        protected const double invg0 = 1d / 9.80665d;

        virtual public void CreateEngine()
        {
            engineSolver = new EngineSolver();
            Need_Area = engineSolver.GetArea();
        }
        protected void SetFlameout()
        {
            CLAMP = 0f;
            flameoutBar = float.MaxValue;
        }
        protected void SetUnflameout()
        {
            // hack to get around my not making CanStart() virtual
            CLAMP = float.MaxValue;
            flameoutBar = 0f;
        }
        virtual public void Start()
        {
            useAtmCurve = atmChangeFlow = useVelCurve = false;
            CreateEngine();
            
            List<Part> parts = null;
            if(HighLogic.LoadedSceneIsEditor)
                parts = EditorLogic.fetch.getSortedShipList();
            else if(HighLogic.LoadedSceneIsFlight)
                parts = vessel.Parts;
            if (parts != null)
            {
                partsCount = parts.Count;
                GetLists(parts);
            }
            currentThrottle = 0f;
            flameout = false;
            SetUnflameout();
            Fields["fuelFlowGui"].guiUnits = " kg/sec";
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            flameout = false;
            SetUnflameout();
            // set initial params
            engineTemp = 288.15d;
            currentThrottle = 0f;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            useAtmCurve = atmChangeFlow = useVelCurve = false;
        }

        new virtual public void FixedUpdate()
        {
            realIsp = 0f;
            finalThrust = 0f;
            fuelFlowGui = 0f;
            requestedThrottle = 0f;

            SetUnflameout();

            if (HighLogic.LoadedSceneIsEditor)
            {
                List<Part> eParts = EditorLogic.fetch.getSortedShipList();
                int eNewCount = eParts.Count;
                if (eNewCount != partsCount)
                {
                    partsCount = eNewCount;
                    GetLists(eParts);
                }
                return;
            }

            if (TimeWarping())
            {
                currentThrottle = 0f;
                return;
            }

            // update our links
            List<Part> parts = vessel.Parts;
            int newCount = parts.Count;
            if(newCount != partsCount)
            {
                partsCount = newCount;
                GetLists(parts);
            }

            if (EngineIgnited)
            {
                UpdatePropellantStatus();
                if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude <= vessel.mainBody.atmosphereDepth)
                {
                    UpdateInletEffects();
                    requestedThrottle = vessel.ctrlState.mainThrottle;
                    UpdateThrottle();
                    UpdateFlightCondition(vessel.altitude, vessel.srfSpeed, vessel.staticPressurekPa, vessel.externalTemperature);
                    CalculateEngineParams();

                    if (fireflag >= 0.79999d)
                    {
                        if (fireflag > 1d)
                        {
                            FlightLogger.eventLog.Add("[" + FormatTime(vessel.missionTime) + "] " + part.partInfo.title + " melted its internals from heat.");
                            part.explode();
                        }
                        if (overheatBox == null)
                        {
                            overheatBox = part.stackIcon.DisplayInfo();
                            overheatBox.SetMsgBgColor(XKCDColors.DarkRed.A(0.6f));
                            overheatBox.SetMsgTextColor(XKCDColors.OrangeYellow.A(0.6f));
                            overheatBox.SetMessage("Engine");
                            overheatBox.SetProgressBarBgColor(XKCDColors.DarkRed.A(0.6f));
                            overheatBox.SetProgressBarColor(XKCDColors.OrangeYellow.A(0.6f));
                        }
                        overheatBox.SetValue((float)fireflag * 2f - 1f, 0.6f, 1.0f);
                    }
                    else
                    {
                        overheatBox = null;
                    }

                    if (finalThrust > 0f)
                    {
                        // now apply the thrust
                        if (part.Rigidbody != null)
                        {
                            int tCount = thrustTransforms.Count;
                            float thrustPortion = finalThrust / tCount;
                            Transform t;
                            for (int i = 0; i < tCount; ++i)
                            {
                                t = thrustTransforms[i];
                                part.Rigidbody.AddForceAtPosition(thrustRot * -t.forward * thrustPortion, t.position + t.rotation * thrustOffset, ForceMode.Force); //Send thrust equally to all nozzles.
                            }
                        }
                        EngineExhaustDamage();

                        double thermalFlux = fireflag * heatProduction * vessel.VesselValues.HeatProduction.value * PhysicsGlobals.InternalHeatProductionFactor * part.thermalMass;
                        part.AddThermalFlux(thermalFlux);
                    }
                }
                else
                {
                    Flameout("No oxygen");
                }
            }
            else
            {

                // Update Gui information
                Events["Shutdown"].active = false;
                Events["Activate"].active = true;
                fuelFlowGui = 0f; // No fuel is flowing, zero out gui values
                realIsp = 0f;
                finalThrust = 0f;

                if (part.ShieldedFromAirstream)
                {
                    status = "Occluded";
                }
                else
                {
                    status = "Off";
                }
                statusL2 = "";
            }
            FXUpdate();
            if (flameout || !EngineIgnited)
            {
                SetFlameout();
            }
        }

        //ferram4: separate out so function can be called separately for editor sims
        virtual public void UpdateInletEffects()
        {
            double EngineArea = 0, InletArea = 0;
            OverallTPR = 0;

            if (engineSolver == null)
            {
                Debug.Log("HOW?!");
                return;
            }
            double M0 = engineSolver.GetM0();
            int eCount = engineList.Count;
            for (int j = 0; j < eCount; ++j)
            {
                ModuleRealEngine e = engineList[j];
                if((object)e != null) // probably unneeded because I'm updating the lists now
                {
                    EngineArea += e.engineSolver.GetArea();
                }
            }
                        
            for (int j = 0; j < inletList.Count; j++)        
            {
                AJEInlet i = inletList[j];
                if ((object)i != null) // probably unneeded because I'm updating the lists now
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

        new virtual public void UpdateThrottle()
        {
            currentThrottle = Mathf.Max(0.01f, currentThrottle);
            actualThrottle = Mathf.RoundToInt(currentThrottle * 100f);
        }

        virtual public void UpdateFlightCondition(double altitude, double vel, double pressure, double temperature)
        {
            Environment = pressure.ToString("N2") + " kPa;" + temperature.ToString("N2") + " K ";

            engineSolver.SetTPR(OverallTPR);
            engineSolver.CalculatePerformance(pressure, temperature, vel, Arearatio, currentThrottle);
        }

        virtual public void CalculateEngineParams()
        {
            if (!engineSolver.CanThrust())//ramjet check
            {
                Flameout("Speed too low");
                return;
            }
            // Heat
            engineTemp = engineSolver.GetEngineTemp();
            fireflag = engineTemp / maxEngineTemp;


            double thrustIn = engineSolver.GetThrust(); //in N
            double isp = engineSolver.GetIsp();
            double producedThrust = 0d;
            double fuelFlow = engineSolver.GetFuelFlow();
            double massFlow = 0d;
            double propellantRecieved = 0d;

            if (thrustIn <= 0d || double.IsNaN(thrustIn))
            {
                if (currentThrottle > 0f && !double.IsNaN(thrustIn))
                {
                    Flameout("Air combustion failed");
                }
                propellantRecieved = 0d;
                realIsp = 0f;
                fuelFlowGui = 0f;
                producedThrust = 0d;
            }
            else
            {
                // calc flow
                double vesselValue = vessel.VesselValues.FuelUsage.value;
                if (vesselValue == 0d)
                    vesselValue = 1d;
                fuelFlow *= vesselValue;

                massFlow = fuelFlow * 0.001d * TimeWarp.fixedDeltaTime; // in tons

                if (CheatOptions.InfiniteFuel == true)
                {
                    propellantRecieved = 1d;
                    UnFlameout();
                }
                else
                {
                    propellantRecieved = RequestPropellant(massFlow);
                }
                producedThrust = thrustIn * propellantRecieved * 0.001d; // to kN
                fuelFlowGui = (float)(fuelFlow * propellantRecieved);
                realIsp = (float)isp;

                // soft cap
                if (producedThrust > thrustUpperLimit)
                    producedThrust = thrustUpperLimit + (producedThrust - thrustUpperLimit) * 0.1d;
            }

            finalThrust = (float)producedThrust * vessel.VesselValues.EnginePower.value;
        }

        protected void GetLists(List<Part> parts)
        {
            engineList.Clear();
            inletList.Clear();
            for (int j = 0; j < partsCount; j++)        //reduces garbage produced compared to foreach due to Unity Mono issues
            {
                Part p = parts[j];
                if (p.Modules.Contains("AJEModule"))
                {
                    engineList.Add((ModuleEnginesAJEJet)p.Modules["AJEModule"]);          //consider storing list of affected AJEModules and AJEInlets, perhaps a single one for each vessel.  Would result in better performance     
                }
                if (p.Modules.Contains("AJEInlet"))
                {
                    inletList.Add((AJEInlet)p.Modules["AJEInlet"]);
                }
            }
        }
        
        new virtual public float normalizedOutput
        {
            get
            {
                // should this just be actualThrottle ?
                // or should we get current thrust divided max possible thrust here?
                // or what? FIXME
                return finalThrust / maxThrust;
            }
        }
        new virtual public string GetModuleTitle()
        {
            return "AJE Engine";
        }
        new virtual public string GetPrimaryField()
        {
            return "";
        }

        public override string GetInfo()
        {
            return "AJE Engine";
        }

        protected static string FormatTime(double time)
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

