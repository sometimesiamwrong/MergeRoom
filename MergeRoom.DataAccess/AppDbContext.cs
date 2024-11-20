using Microsoft.EntityFrameworkCore;
using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;

namespace DataAccess;

/// <summary>
/// App db context.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Db context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// User 
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Merge request
    /// </summary>
    public DbSet<MergeRequest> MergeRequests { get; set; }

    /// <summary>
    /// Projects
    /// </summary>
    public DbSet<Project> Projects { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public DbSet<Note> Notes { get; set; }

    /// <summary>
    /// Diff notes
    /// </summary>
    public DbSet<DiffNote> DiffNotes { get; set; }

    /// <summary>
    /// Jobs
    /// </summary>
    public DbSet<Job> Jobs { get; set; }

    /// <summary>
    /// Resource state events
    /// </summary>
    public DbSet<ResourceStateEvent> ResourceStateEvents { get; set; }

    /// <summary>
    /// Approve infos
    /// </summary>
    public DbSet<ApproveInfo> ApproveInfos { get; set; }
}
