namespace XyloCode.EFCore.Extensions
{
    public enum DeletionMode
    {
        None = 0,
        FullDeletion = 1,
        DeferredDeletion = 2,
        DeferredDeletionIfPossibleElseNone = 3, 
        DeferredDeletionIfPossibleElseFullDeletion = 4,
        CustomDeletion = 5
    }
}
