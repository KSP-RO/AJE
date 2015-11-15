using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AJE
{
    class ModuleAJEJetAnimateNozzle : PartModule
    {
        [KSPField]
        public string animationName = "throttleAnim";

        [KSPField]
        public int layer = 1;

        [KSPField]
        public float responseSpeed = 0.5f;

        [KSPField]
        public float minRelativeArea = 0.3f;

        [KSPField]
        public float maxRelativeArea = 1.0f;

        [KSPField]
        public bool useAnimCurve = false;

        [KSPField]
        public FloatCurve animCurve = new FloatCurve();

        // [KSPField(guiActive = true)]
        protected float relativeNozzleArea = 0f;

        // [KSPField(guiActive = true)]
        protected float animationState = 0f;

        protected ModuleEnginesAJEJet engineModule;
        protected AnimationState[] animStates;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            FindEngineModule();
            FindAnimations();

            animationState = 0f;
        }

        protected virtual void FindEngineModule()
        {
            engineModule = part.FindModuleImplementing<ModuleEnginesAJEJet>();

            if (engineModule == null)
                Debug.LogError("Error: Cannot find ModuleEnginesAJEJet on part " + part.name);
        }

        protected virtual void FindAnimations()
        {
            Animation[] anims = part.FindModelAnimators(animationName);
            if (anims.Length == 0)
                Debug.LogError("Error: Cannot find animation named '" + animationName + "' on part " + part.name);

            animStates = new AnimationState[anims.Length];
            for (int i = 0; i < anims.Length; i++)
            {
                Animation anim = anims[i];
                AnimationState animState = anim[animationName];
                animState.speed = 0;
                animState.enabled = true;
                animState.layer = layer;
                anim.Play(animationName);
                animStates[i] = animState;
            }
        }

        protected virtual void HandleResponseTime(float input)
        {
            animationState = Mathf.Lerp(animationState, input, responseSpeed * 25f * TimeWarp.fixedDeltaTime);
        }

        public void FixedUpdate()
        {
            if (engineModule != null)
                relativeNozzleArea = engineModule.GetRelativeNozzleArea();
            else
                relativeNozzleArea = 0f;

            float newState = Mathf.Clamp(relativeNozzleArea, minRelativeArea, maxRelativeArea);
            newState = Mathf.InverseLerp(minRelativeArea, maxRelativeArea, newState);
            if (useAnimCurve)
                newState = animCurve.Evaluate(newState);

            HandleResponseTime(newState);

            for (int i = 0; i < animStates.Length; i++)
            {
                animStates[i].normalizedTime = animationState;
            }
        }
    }
}
