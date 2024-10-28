using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling
{
    public class HandleData
    {
        public MergeRequest? OldMergeRequest { get; set; }
        public MergeRequest NewMergeRequest { get; set; } = null!;
        public List<Note> Notes { get; set; } = new List<Note>();
        public Project Project { get; set; } = null!;
    }
}
