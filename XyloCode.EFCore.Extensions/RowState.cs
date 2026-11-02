namespace XyloCode.EFCore.Extensions
{
    public enum RowState
    {
        NORMAL = 0,
        FREEZE = 0x10,
        DELETED = 0x7F,


        UPDATABLE = 0x80,
        SKIP = 0x81,
        UPDATED = 0x82,
        INSERTED = 0x83,

    }
}
