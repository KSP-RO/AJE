using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SolverEngines;

namespace AJE.AnimationModules
{
    public class ModuleAJEJetAnimateNozzleArea : ModuleAnimateSolverEngine<ModuleEnginesAJEJet>
    {
        [KSPField]
        public float minArea = 1f;

        [KSPField]
        public float maxArea = 0.5f;

        [KSPField]
        public bool calculateAreas = true;

        [KSPField]
        public float maxAreaStaticHeadroom = 1.5f;

        [KSPField]
        public float idleState = 1f;

        [KSPField]
        public float idleThreshold = 0.01f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            HandleCalculateAreas();

            Debug.Log("Min area: " + minArea.ToString());
            Debug.Log("Max area: " + maxArea.ToString());
        }

        public void HandleCalculateAreas()
        {
            if (!calculateAreas) return;
            if (engine == null)
            {
                LogError("Cannot calculate areas: engine is null!");
                return;
            }

            float minStaticArea = engine.GetStaticDryNozzleArea();
            float maxStaticArea = engine.GetStaticWetNozzleArea();

            minArea = minStaticArea;
            maxArea = minStaticArea + (maxStaticArea - minStaticArea) * (1f + maxAreaStaticHeadroom);
        }

        public override float TargetAnimationState()
        {
            if (!engine.isOperational || HighLogic.LoadedSceneIsEditor)
            {
                return defaultState;
            }
            else if (engine.GetCoreThrottle() < idleThreshold)
            {
                return idleState;
            }
            else
            {
                float area = engine.GetNozzleArea();
                return Mathf.InverseLerp(minArea, maxArea, area);
            }
        }
    }
}
