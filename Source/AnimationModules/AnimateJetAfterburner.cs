using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SolverEngines;

namespace AJE.AnimationModules
{
    public class ModuleAJEJetAnimateAfterburner : ModuleAnimateSolverEngine<ModuleEnginesAJEJet>
    {
        public override float TargetAnimationState()
        {
            return engine.GetABThrottle();
        }
    }
}
