using Microsoft.EntityFrameworkCore;

namespace TODO;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TodoTask> Tasks { get; set; } // Ваша таблица в БД
}
