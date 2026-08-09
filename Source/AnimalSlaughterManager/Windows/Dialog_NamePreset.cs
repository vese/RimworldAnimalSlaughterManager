using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>A simple modal dialog that asks the user for a preset name.</summary>
    public class Dialog_NamePreset : Window
    {
        private string nameBuffer = "";
        private readonly Action<string> onSave;

        public override Vector2 InitialSize => new Vector2(400f, 150f);

        public Dialog_NamePreset(Action<string> onSave)
        {
            this.onSave = onSave;
            doCloseX = true;
            draggable = true;
            absorbInputAroundWindow = true;
            optionalTitle = ASMKeys.PresetNameTitle.Translate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float y = inRect.y;
            nameBuffer = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 28f), nameBuffer);
            y += 36f;
            bool canSave = !nameBuffer.Trim().NullOrEmpty();
            if (Widgets.ButtonText(new Rect(inRect.xMax - 120f, y, 120f, 28f), ASMKeys.SavePreset.Translate(), active: canSave) && canSave)
            {
                onSave(nameBuffer.Trim());
                Close();
            }
        }
    }
}
