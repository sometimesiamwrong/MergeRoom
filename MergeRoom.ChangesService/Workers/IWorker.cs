using MergeRoom.Domain.Entities;

namespace MergeRoom.ChangesService.Workers
{
    public interface IWorker
    {
        Task ExecuteAsync(List<Project> project);
    }
}
