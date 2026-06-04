using JobQueue.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
}