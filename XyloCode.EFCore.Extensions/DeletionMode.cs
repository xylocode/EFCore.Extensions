namespace XyloCode.EFCore.Extensions
{
    public enum DeletionMode
    {
        None = 0,
        FullDeletion = 1,
        DeferredDeletion = 2,
        DeferredDeletionIfPossibleElseFullDeletion = 3,
        CustomDeletion = 4
    }
}
