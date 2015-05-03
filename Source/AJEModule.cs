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


    public class ModuleEnginesAJEJet : ModuleEnginesFX, IModuleInfo
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
        public float maxT3 = 9999;
        [KSPField(isPersistant = false, guiActive = true)]
        public String Environment;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Throttle", guiUnits = "%")]
        public int actualThrottle;

        [KSPField(isPersistant = false)]
        public double thrustUpperLimit = double.MaxValue;

        // engine temp stuff
        [KSPField(isPersistant = false, guiActive = true, guiName = "Engine Temp", guiUnits = " K")]
        public float engineTemp = 0f;
        protected VInfoBox overheatBox = null;


        public AJESolver aje = null;
        public List<ModuleEnginesAJEJet> engineList;
        public List<AJEInlet> inletList;
        public double OverallTPR = 1d, Arearatio = 1d;

        protected int partsCount = 0;

        private const double invg0 = 1d / 9.80665d;

        public void CreateEngine()
        {
            Need_Area = Area * (1 + BPR);
            //           bool DREactive = AssemblyLoader.loadedAssemblies.Any(
            //               a => a.assembly.GetName().Name.Equals("DeadlyReentry.dll", StringComparison.InvariantCultureIgnoreCase));
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
        }
        public void Start()
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
            // hack to get around my not making CanStart() virtual
            CLAMP = float.MaxValue;
            flameoutBar = 0f;
            flameout = false;
            Fields["fuelFlowGui"].guiUnits = " kg/sec";
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            // hack to get around my not making CanStart() virtual
            CLAMP = float.MaxValue;
            flameoutBar = 0f;
            flameout = false;
            // set initial params
            engineTemp = 288.15f;
            currentThrottle = 0f;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            useAtmCurve = atmChangeFlow = useVelCurve = false;
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                List<Part> parts = EditorLogic.fetch.getSortedShipList();
                int newCount = parts.Count;
                if(newCount != partsCount)
                {
                    partsCount = newCount;
                    GetLists(parts);
                }
            }
        }
              
        new public void FixedUpdate()
        {
            realIsp = 0f;
            finalThrust = 0f;
            fuelFlowGui = 0f;
            requestedThrottle = 0f;

            // hack to get around my not making CanStart() virtual
            CLAMP = float.MaxValue;
            flameoutBar = 0f;

            if (HighLogic.LoadedSceneIsEditor || TimeWarping())
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

                    double thrustOut = 0d;
                    if (CPR != 1 || aje.GetM0() >= 0.3)//ramjet check
                        thrustOut = CalculateEngineParams();
                    else
                    {
                        Flameout("Speed too low");
                    }

                    finalThrust = (float)thrustOut * vessel.VesselValues.EnginePower.value;

                    // Heat
                    double dEngineTemp = aje.GetT3();
                    engineTemp = (float)dEngineTemp;
                    double fireflag = dEngineTemp / maxT3;
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
                        // hack to get around my not making CanStart() virtual
                        CLAMP = float.MaxValue;
                        flameoutBar = 0f;

                        // now apply the thrust
                        if (part.Rigidbody != null)
                        {
                            int tCount = thrustTransforms.Count;
                            float thrustPortion = finalThrust / tCount;
                            Transform t;
                            for (int i = 0; i < tCount; ++i)
                            {
                                t = thrustTransforms[i];
                                part.Rigidbody.AddForceAtPosition(-t.forward * thrustPortion, t.position, ForceMode.Force); //Send thrust equally to all nozzles.
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
                // hack to get around my not making CanStart() virtual
                CLAMP = 0.0001f;
                flameoutBar = float.MaxValue;
            }
        }

        //ferram4: separate out so function can be called separately for editor sims
        public void UpdateInletEffects()
        {
            double EngineArea = 0, InletArea = 0;
            OverallTPR = 0;

            if (aje == null)
            {
                Debug.Log("HOW?!");
                return;
            }
            double M0 = aje.GetM0();
            int eCount = engineList.Count;
            for (int j = 0; j < eCount; ++j)
            {
                ModuleEnginesAJEJet e = engineList[j];
                if((object)e != null) // probably unneeded because I'm updating the lists now
                {
                    EngineArea += e.Area * (1 + e.BPR);
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

        new public void UpdateThrottle()
        {
            if (CPR != 1)
            {
                double requiredThrottle = requestedThrottle * thrustPercentage * 0.01d;
                double deltaT = TimeWarp.fixedDeltaTime;
                double throttleResponseRate = Math.Max(2 / Area / (1 + BPR), 5) * 0.01d; //percent per second

                double d = requiredThrottle - currentThrottle;
                if (Math.Abs(d) > throttleResponseRate * deltaT)
                    currentThrottle += Mathf.Sign((float)d) * (float)(throttleResponseRate * deltaT);
                else
                    currentThrottle = (float)requiredThrottle;
            }
            else // ramjet
            {
                currentThrottle = (float)(requestedThrottle * thrustPercentage * 0.01);
            }
            currentThrottle = Mathf.Max(0.01f, currentThrottle);
            actualThrottle = Mathf.RoundToInt(currentThrottle * 100f);
        }

        public void UpdateFlightCondition(double altitude, double vel, double pressure, double temperature)
        {
            Environment = pressure.ToString("N2") + " kPa;" + temperature.ToString("N2") + " K ";

            aje.SetTPR(OverallTPR);
            aje.CalculatePerformance(pressure, temperature, vel, currentThrottle);
            
        }

        public double CalculateEngineParams()
        {
            double thrustIn = aje.GetThrust() * Arearatio; //in N
            double isp = aje.GetIsp();
            double producedThrust = 0d;
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
                double fuelFlow = thrustIn * invg0 / isp * vesselValue;
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
                fuelFlowGui = (float)(fuelFlow * propellantRecieved * vesselValue);
                realIsp = (float)isp;

                // soft cap
                if (producedThrust > thrustUpperLimit)
                    producedThrust = thrustUpperLimit + (producedThrust - thrustUpperLimit) * 0.1d;
            }

            

            return producedThrust;
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
        public string GetStaticThrustInfo()
        {
            string output = "";
            if(aje == null)
                CreateEngine();

            // get stats
            double pressure = 101.325d, temperature = 288.15d;
            if (Planetarium.fetch != null)
            {
                CelestialBody home = Planetarium.fetch.Home;
                if (home != null)
                {
                    pressure = home.GetPressure(0d);
                    temperature = home.GetTemperature(0d);
                }
            }

            currentThrottle = 1f;
            OverallTPR = 1d;

            UpdateFlightCondition(0d, 0d, pressure, temperature);
            double thrust = (aje.GetThrust() * 0.001d);

            if (TAB == 0) // no AB
            {
                output += "<b>Static Thrust: </b>" + thrust.ToString("N2") + " kN, <b>SFC: </b>" + (1d / aje.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h";
            }
            else
            {
                if (CPR == 1) // ramjet
                {
                    output += "<b>Thrust: </b>Ramjet of area " + Area + " m^2";
                    if (thrustUpperLimit != double.MaxValue)
                        output += ", max rated thrust " + thrustUpperLimit.ToString("N2") + " kN";
                }
                else
                {
                    output += "<b>Static Thrust (wet): </b>" + thrust.ToString("N2") + " kN, <b>SFC: </b>" + (1d / aje.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h";
                    currentThrottle = 2f / 3f;
                    UpdateFlightCondition(0d, 0d, pressure, temperature);
                    thrust = (aje.GetThrust() * 0.001d);
                    output += "\n<b>Static Thrust (dry): </b>" + thrust.ToString("N2") + " kN, <b>SFC: </b>" + (1d / aje.GetIsp() * 3600d).ToString("N4") + " kg/kgf-h";
                }
            }
            return output;
        }
        new public float normalizedOutput
        {
            get
            {
                // should this just be actualThrottle ?
                // or should we get current thrust divided max possible thrust here?
                // or what? FIXME
                return finalThrust / maxThrust;
            }
        }
        new public string GetModuleTitle()
        {
            return "AJE Engine";
        }
        new public string GetPrimaryField()
        {
            return GetStaticThrustInfo();
        }

        public override string GetInfo()
        {
            string output = GetStaticThrustInfo();

            output += "\n<b><color=#99ff00ff>Propellants:</color></b>\n";
            Propellant p;
            string pName;
            for (int i = 0; i < propellants.Count; ++i)
            {
                p = propellants[i];
                pName = KSPUtil.PrintModuleName(p.name);

                output += "- <b>" + pName + "</b>: " + getMaxFuelFlow(p).ToString("0.0##") + "/sec. Max.\n";
                output += p.GetFlowModeDescription();
            }
            output += "<b>Flameout under: </b>" + (ignitionThreshold * 100f).ToString("0.#") + "%\n";

            if (!allowShutdown) output += "\n" + "<b><color=orange>Engine cannot be shut down!</color></b>";
            if (!allowRestart) output += "\n" + "<b><color=orange>If shutdown, engine cannot restart.</color></b>";
            
            currentThrottle = 0f;
            
            return output;
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

