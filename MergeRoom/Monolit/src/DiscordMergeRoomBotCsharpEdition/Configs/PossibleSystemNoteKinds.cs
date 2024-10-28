namespace DiscordMergeRoomBotCsharpEdition.Configs
{
    public class PossibleSystemNoteKinds
    {
        public string ResolvedAllThreads { get; init; } = null!;

        public string Approve { get; init; } = null!;

        public string Unapprove { get; init; } = null!;

        public List<string> AllKinds => new List<string> { ResolvedAllThreads, Approve, Unapprove };
    }
}
