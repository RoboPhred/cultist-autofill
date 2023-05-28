using System;
using System.Collections.Generic;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Spheres;
using SecretHistories.Fucine;
using SecretHistories.UI;
using SecretHistories.Services;
using UnityEngine;
using UnityEngine.InputSystem;

public class CultistAutofill : MonoBehaviour, ISettingSubscriber
{
    private static readonly string KeySetting = "autofill_trigger";

    private Key binding;

    void Start()
    {
        NoonUtility.LogWarning($"Hello World! {this.GetType().Name}");
        var setting = Watchman.Get<Compendium>().GetEntityById<Setting>(CultistAutofill.KeySetting);
        setting.AddSubscriber(this);
        this.ReadBinding();
    }

    void OnDestroy()
    {
        var setting = Watchman.Get<Compendium>().GetEntityById<Setting>(CultistAutofill.KeySetting);
        setting.RemoveSubscriber(this);
    }

    public static void Initialise()
    {
        try
        {
            var component = new GameObject().AddComponent<CultistAutofill>();
        }
        catch (Exception e)
        {
            NoonUtility.LogException(e);
        }
    }

    void Update()
    {
        if (Keyboard.current[this.binding].wasPressedThisFrame)
        {
            NoonUtility.LogWarning($"{this.GetType().Name} Hotkey");
            this.AutofillSituation();
        }
    }

    void ReadBinding()
    {
        NoonUtility.LogWarning($"{this.GetType().Name} Reading binding");
        var setting = Watchman.Get<Compendium>().GetEntityById<Setting>(CultistAutofill.KeySetting).CurrentValue as string;
        NoonUtility.LogWarning($"{this.GetType().Name} AutoFill setting is {setting}");
        var keyStr = setting.Split('/')[1];
        try
        {
            this.binding = (Key)Enum.Parse(typeof(Key), keyStr);
            NoonUtility.LogWarning($"{this.GetType().Name} AutoFill bound successfully");
        }
        catch (Exception e)
        {
            NoonUtility.LogException(e);
            this.binding = Key.None;
        }
    }

    void ISettingSubscriber.BeforeSettingUpdated(object newValue)
    {

    }

    void ISettingSubscriber.WhenSettingUpdated(object newValue)
    {
        var strValue = newValue as string;
        Watchman.Get<Config>().PersistConfigValue(CultistAutofill.KeySetting, strValue);
        this.binding = (Key)Enum.Parse(typeof(Key), strValue);
    }

    void AutofillSituation()
    {
        if (Watchman.Get<Numa>().IsOtherworldActive())
        {
            return;
        }

        var situation = this.GetOpenSituation();
        if (situation == null)
        {
            return;
        }

        var candidates = this.GetTokensOrderedByDistance(situation.GetRectTransform().anchoredPosition).ToArray();

        this.TryFillSpheres(() => situation.GetSpheresActiveForCurrentState(), candidates);
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