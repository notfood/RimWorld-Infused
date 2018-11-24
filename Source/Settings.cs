using System;
using UnityEngine;
using Verse;

namespace Infused
{
    public class Settings : ModSettings
    {
        public static float mult = 1f;
        public static float bias = 0.5f;

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard list = new Listing_Standard(GameFont.Small);
            list.ColumnWidth = rect.width;
            list.Begin(rect);
            list.Label(ResourceBank.Strings.SettingsMultiplier + ":" + mult.ToString("0.00"), -1, ResourceBank.Strings.SettingsMultiplierDesc);
            mult = list.Slider(mult, 0.05f, 4f);
            list.Label(ResourceBank.Strings.SettingsBias + ":" + bias.ToString("0.00"), -1, ResourceBank.Strings.SettingsBiasDesc);
            bias = list.Slider(bias, 0.01f, 6f);
            list.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref mult, "ChanceMult");
            Scribe_Values.Look(ref bias, "ChanceBias");
        }
    }
}
