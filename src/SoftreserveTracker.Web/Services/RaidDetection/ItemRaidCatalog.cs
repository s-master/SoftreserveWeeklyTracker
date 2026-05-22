using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.RaidDetection;

/// <summary>
/// Known TBC Phase 2 loot item IDs grouped by raid for Gargul-only session detection.
/// </summary>
public static class ItemRaidCatalog
{
    private static readonly HashSet<int> TkItemIds =
    [
        29918, 29920, 29921, 29922, 29923, 29924, 29925, 29947, 29948, 29965, 29966, 29972, 29977, 29981, 29982,
        29983, 29985, 29986, 29988, 29993, 29996, 30236, 30237, 30248, 30249, 30250, 30447, 30448, 30449, 30450,
        30619, 32267, 32405, 32458, 32515, 32944
    ];

    private static readonly HashSet<int> SscItemIds =
    [
        30020, 30022, 30025, 30027, 30047, 30049, 30050, 30052, 30055, 30057, 30058, 30059, 30062, 30063, 30065,
        30067, 30075, 30080, 30081, 30082, 30085, 30091, 30092, 30095, 30096, 30100, 30104, 30105, 30106, 30107,
        30108, 30109, 30110, 30239, 30240, 30241, 30242, 30243, 30244, 30245, 30246, 30247, 30303, 30305, 30306,
        30626, 30627, 30629, 30664, 30720, 33055
    ];

    public static RaidType DetectFromItemIds(IEnumerable<int> itemIds)
    {
        var tk = 0;
        var ssc = 0;
        foreach (var id in itemIds)
        {
            if (TkItemIds.Contains(id))
            {
                tk++;
            }
            else if (SscItemIds.Contains(id))
            {
                ssc++;
            }
        }

        if (tk == 0 && ssc == 0)
        {
            return RaidType.Ssc;
        }

        return tk >= ssc ? RaidType.Tk : RaidType.Ssc;
    }
}
