using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SolverEngines;

namespace AJE.AnimationModules
{
    public class ModuleAJEJetAnimateExhaustHeat : ModuleAnimateSolverEngine<ModuleEnginesAJEJet>
    {
        [KSPField]
        public float minTemp = 798.0f;

        [KSPField]
        public float maxTemp = 1500.0f;

        public override float TargetAnimationState()
        {
            float temperature = engine.GetEmissiveTemp();

            return Mathf.InverseLerp(minTemp, maxTemp, temperature);
        }
    }
}
