using UnityEngine;
using SolverEngines;

namespace AJE.AnimationModules
{
    public interface INozzleArea : IEngineStatus
    {
        float GetNozzleArea();
    }
    
    public class ModuleAJEJetAnimateNozzleArea : ModuleAnimateSolverEngine<INozzleArea>
    {
        [KSPField]
        public float minArea = 0.5f;

        [KSPField]
        public float maxArea = 1f;

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
        }

        public void HandleCalculateAreas()
        {
            if (!calculateAreas) return;
            if (engine == null)
            {
                LogError("Cannot calculate areas: engine is null!");
                return;
            }

            if (engine is ModuleEnginesAJEJet jetEngine)
            {
                float minStaticArea = jetEngine.GetStaticDryNozzleArea();
                float maxStaticArea = jetEngine.GetStaticWetNozzleArea();
                minArea = minStaticArea;
                maxArea = minStaticArea + (maxStaticArea - minStaticArea) * (1f + maxAreaStaticHeadroom);

                Debug.Log("Min area: " + minArea.ToString());
                Debug.Log("Max area: " + maxArea.ToString());
            }
            else
            {
                LogError("Engine is not ModuleEnginesAJEJet, cannot fit area");
            }

        }

        public override float TargetAnimationState()
        {
            if (!engine.isOperational || HighLogic.LoadedSceneIsEditor)
            {
                return defaultState;
            }
            else if (engine.normalizedOutput < idleThreshold)
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
