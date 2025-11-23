using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using LethalMin.Utils;
using LethalMin.Routeing;

namespace LethalMin.Pikmin
{
    [CreateAssetMenu(fileName = "PikminMoonSettings", menuName = "Pikmin/MoonSettings", order = 0)]
    public class MoonSettings : ScriptableObject
    {
        public SelectableLevel Level = null!;

        public bool OverridePathing = false;

        // Optional custom predicate for advanced routing conditions
        public System.Func<PikminRouteRequest, RouteContext, bool>? CustomPathingCondition = null;

        public bool CanSettingsHandlePathing(PikminRouteRequest request, RouteContext context)
        {
            // If custom condition is defined, use it; otherwise fall back to OverridePathing
            return CustomPathingCondition?.Invoke(request, context) ?? OverridePathing;
        }
    }
}