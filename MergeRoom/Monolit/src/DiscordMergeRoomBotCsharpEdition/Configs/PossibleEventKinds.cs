namespace DiscordMergeRoomBotCsharpEdition.Configs
{
    public class PossibleEventKinds
    {
        public string MergeRequest { get; set; } = null!;

        public string Note { get; set; } = null!;

        public List<string> AllKinds => new List<string> { MergeRequest, Note };
    }
}
