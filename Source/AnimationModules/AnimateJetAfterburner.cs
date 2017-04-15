using SolverEngines;

namespace AJE.AnimationModules
{
    public interface IJetAfterburner : IEngineStatus
    {
        float GetABThrottle();
    }

    public class ModuleAJEJetAnimateAfterburner : ModuleAnimateSolverEngine<IJetAfterburner>
    {
        public override float TargetAnimationState()
        {
            return engine.GetABThrottle();
        }
    }
}
