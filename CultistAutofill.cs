using System;
using System.Collections.Generic;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CultistAutofill
{
    [BepInEx.BepInPlugin("net.robophreddev.CultistSimulator.CultistAutofill", "CultistAutofill", "1.0.1")]
    public class CultistAutofillMod : BepInEx.BaseUnityPlugin
    {
        void Start()
        {
            this.Logger.LogInfo("CultistAutofill initialized.");
        }

        void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                this.AutofillSituation();
            }
        }

        void AutofillSituation()
        {
            // if (TabletopManager.IsInMansus())
            // {
            //     return;
            // }

            var situation = this.GetOpenSituation();
            if (situation == null)
            {
                return;
            }

            var candidates = this.GetTokensOrderedByDistance(situation.GetRectTransform().anchoredPosition).ToArray();

            var activeSpheres = situation.GetSpheresActiveForCurrentState();
            this.TryFillSpheres(() => activeSpheres, candidates);
        }

        void TryFillSpheres(Func<IList<Sphere>> slotsResolver, IList<Token> candidates)
        {
            // We handle the first slot independently, as chooing a first slot will
            //  usually determine what other slots are available.
            var primarySlot = slotsResolver().FirstOrDefault();
            if (primarySlot == null)
            {
                return;
            }

            if (!this.TrySatisfySphere(primarySlot, candidates))
            {
                return;
            }

            // Fill the remaining slots.
            //  We need to re-fetch the slots, as slotting the primary may
            //  provide us with new slots.
            var slots = slotsResolver();
            foreach (var slot in slots.Skip(1))
            {
                this.TrySatisfySphere(slot, candidates);
            }
        }

        bool TrySatisfySphere(Sphere sphere, IEnumerable<Token> candidates)
        {
            if (sphere.GetElementStacks().Count > 0)
            {
                // Already something in the sphere.
                return true;
            }

            foreach (var token in candidates)
            {
                // TryAcceptToken handles UI concerns like messages and sound effects, so we need to check manually
                // to avoid spamming sound effects.
                if (sphere.GetMatchForTokenPayload(token.Payload).MatchType == SlotMatchForAspectsType.Okay)
                {
                    // We sitll need to TryAcceptToken, as that contains the logic to split the stack into one card.
                    sphere.TryAcceptToken(token, new Context(Context.ActionSource.PlayerDrag));
                    return true;
                }
            }

            return false;
        }

        IEnumerable<Token> GetTokensOrderedByDistance(Vector2 fromPoint)
        {
            return from sphere in Watchman.Get<HornedAxe>().GetSpheres()
                   where sphere.SphereCategory == SphereCategory.World
                   from stack in sphere.GetElementStacks()
                   orderby CalcDistance(stack.GetRectTransform().anchoredPosition, fromPoint)
                   select stack.Token;
        }

        float CalcDistance(Vector2 a, Vector2 b)
        {
            var xDist = a.x - b.x;
            var yDist = a.y - b.y;
            return (float)Math.Sqrt((xDist * xDist) + (yDist * yDist));
        }

        Situation GetOpenSituation()
        {
            return Watchman.Get<HornedAxe>().GetRegisteredSituations().Find(x => x.IsOpen);
        }
    }
}