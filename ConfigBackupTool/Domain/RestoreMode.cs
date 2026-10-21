namespace ConfigBackupTool.Domain
{
    public enum RestoreMode
    {
        ReplaceAll,
        AddMissingOnly,
        AskOnConflict
    }
}
