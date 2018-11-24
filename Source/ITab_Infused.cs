using System.Text;

using RimWorld;
using UnityEngine;
using Verse;

namespace Infused
{
    public class ITab_Infused : ITab
    {
        static readonly Vector2 WinSize = new Vector2(400, 550);

        public override bool IsVisible => SelThing?.TryGetComp<CompInfused>()?.IsActive ?? false;

        public ITab_Infused()
        {
            size = WinSize;
            labelKey = "Infused.Tab";
        }

        protected override void FillTab()
        {
            var selectedCompInfusion = SelThing.TryGetComp<CompInfused>();

            Text.Font = GameFont.Medium;
            GUI.color = selectedCompInfusion.InfusedLabelColor;

            //Label
            var rectBase = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            var rectLabel = rectBase;

            var label = selectedCompInfusion.GetInfusionLabel(false).CapitalizeFirst();
            Widgets.Label(rectLabel, label);

            //Quality
            var rectQuality = rectBase;
            rectQuality.yMin += Text.CalcHeight(label, rectBase.width);
            Text.Font = GameFont.Small;
            QualityCategory qc;
            selectedCompInfusion.parent.TryGetQuality(out qc);

            var subLabelBuilder = new StringBuilder();
            subLabelBuilder.Append(qc.GetLabel().CapitalizeFirst())
                        .Append(" ")
                        .Append(ResourceBank.Strings.Quality)
                        .Append(" ");
            if (selectedCompInfusion.parent.Stuff != null)
            {
                subLabelBuilder.Append(selectedCompInfusion.parent.Stuff.LabelAsStuff).Append(" ");
            }
            subLabelBuilder.Append(selectedCompInfusion.parent.def.label);
            var subLabel = subLabelBuilder.ToString();

            Widgets.Label(rectQuality, subLabel);
            GUI.color = Color.white;

            //Infusion descriptions
            Text.Anchor = TextAnchor.UpperLeft;
            var rectDesc = rectBase;
            rectDesc.yMin += rectQuality.yMin + Text.CalcHeight(subLabel, rectBase.width);
            Text.Font = GameFont.Small;
            Widgets.Label(rectDesc, selectedCompInfusion.GetDescriptionInfused());
        }
    }
}
