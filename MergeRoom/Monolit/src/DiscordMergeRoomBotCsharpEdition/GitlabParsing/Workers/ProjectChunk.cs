using DiscordMergeRoomBotCsharpEdition.Entities;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers
{
    public class ProjectChunk
    {
        public List<Project> Projects { get; set; } = null!;

        public int ChunkNumber { get; init; }
    }
}
