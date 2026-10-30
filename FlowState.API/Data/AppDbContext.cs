using FlowState.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowState.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}