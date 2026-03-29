using Microsoft.EntityFrameworkCore;

namespace IvyAskStatistics.Connections;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<QuestionEntity> Questions { get; set; }
}
