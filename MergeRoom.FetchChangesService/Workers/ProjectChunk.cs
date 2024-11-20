using MergeRoom.Domain.Entities;

namespace MergeRoom.ChangesService.Workers
{
    public class ProjectChunk
    {
        public List<Project> Projects { get; set; } = null!;

        public int ChunkNumber { get; init; }
    }
}
