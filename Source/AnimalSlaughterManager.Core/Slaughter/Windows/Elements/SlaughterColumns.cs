namespace ASM;

/// <summary>The slaughter table's column geometry, shared by the header and the rows.</summary>
internal static class SlaughterColumns
{
    public const float RowH = 30f;
    public const float HeaderTopH = 22f;
    public const float HeaderSubH = 22f;
    public const float HeaderH = HeaderTopH + HeaderSubH;

    public const float XIcon = 0f, WIcon = 32f;
    public const float XName = 32f, WName = 110f;            // name group (no top bg, no top title)
    public const float XTotC = 142f, XTotM = 202f, WCol = 60f;   // Всего 142..262
    public const float XAMC = 262f, XAMM = 322f;             // Взрослые самцы 262..382
    public const float XYMC = 382f, XYMM = 442f;             // Молодые самцы 382..502
    public const float XAFC = 502f, XAFM = 562f;             // Взрослые самки 502..622
    public const float XYFC = 622f, XYFM = 682f;             // Молодые самки 622..742
    public const float XPregC = 742f, XPregT = 802f, WToggle = 72f;   // Беременные 742..874
    public const float XBondC = 874f, XBondT = 934f;         // С привязанностью 874..1006
    public const float XProtT = 1006f, XProtI = 1086f, WProt = 80f;   // Защита 1006..1166
    public const float XConfig = 1166f, WConfig = 100f;
    public const float TableWidth = 1266f;
}
