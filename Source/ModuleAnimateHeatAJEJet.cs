using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AJE
{
    class ModuleAnimateHeatAJEJet : ModuleAnimateHeat
    {
        [KSPField]
        public string engineID = null;

        // [KSPField(guiActive = true)]
        protected double emissiveTemp = 0d;

        private ModuleEnginesAJEJet engineModule;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            engineModule = null;

            if (string.IsNullOrEmpty(engineID))
            {
                engineModule = part.FindModuleImplementing<ModuleEnginesAJEJet>();

                if (engineModule == null)
                    Debug.LogError("Error: ModuleAnimateHeatAJEJet on part " + part.name + ": unable to find ModuleEnginesAJEJet");
            }
            else
            {
                for (int i = 0; i < part.Modules.Count; i++)
                {
                    PartModule m = part.Modules[i];
                    if (m is ModuleEnginesAJEJet && (m as ModuleEngines).engineID == engineID)
                    {
                        engineModule = m as ModuleEnginesAJEJet;
                        break;
                    }
                }

                if (engineModule == null)
                    Debug.LogError("Error: ModuleAnimateHeatAJEJet on part " + part.name + ": unable to find ModuleEnginesAJEJet with engineID '" + engineID + "'");
            }

            emissiveTemp = 0d;
        }

        new virtual public void Update()
        {
            UpdateHeatEffect();
        }
        
        protected virtual void UpdateHeatEffect()
        {
            if (heatAnimStates == null)
                return;
            
            if (engineModule == null)
                emissiveTemp = part.temperature;
            else
                emissiveTemp = engineModule.GetEmissiveTemp();

            this.SetState(emissiveTemp);

            animState = Mathf.Clamp(animState, 0f, 1f);

            for (int i = 0; i < stateCount; i++)
                heatAnimStates[i].normalizedTime = animState;
        }

    }
}
