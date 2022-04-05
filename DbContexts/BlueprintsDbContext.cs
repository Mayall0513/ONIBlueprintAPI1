using BlueprintAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BlueprintAPI.DbContexts {
    public class BlueprintsDbContext : DbContext {
        public DbSet<Blueprint> Blueprints { get; set; }
        public DbSet<BlueprintVersion> BlueprintVersions { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionJoin> CollectionJoins { get; set; }
      
        public BlueprintsDbContext(DbContextOptions<BlueprintsDbContext> options) : base(options) {  }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Blueprint>().ToTable("blueprints");
            modelBuilder.Entity<Blueprint>().HasOne(x => x.Author).WithMany(x => x.Blueprints);

            modelBuilder.Entity<BlueprintVersion>().ToTable("blueprintversions");
            modelBuilder.Entity<BlueprintVersion>().HasOne(x => x.Blueprint).WithMany(x => x.Versions).IsRequired();
            modelBuilder.Entity<BlueprintVersion>().HasOne(x => x.Blueprint).WithMany(x => x.Versions).IsRequired();

            modelBuilder.Entity<User>().ToTable("users");

            modelBuilder.Entity<Collection>().ToTable("collections");
            modelBuilder.Entity<Collection>().HasOne(x => x.Author).WithMany(x => x.Collections).IsRequired();

            modelBuilder.Entity<CollectionJoin>().ToTable("collectionjoins");
            modelBuilder.Entity<CollectionJoin>().HasOne(x => x.Blueprint).WithMany(x => x.Collections).IsRequired();
            modelBuilder.Entity<CollectionJoin>().HasOne(x => x.Collection).WithMany(x => x.Blueprints).IsRequired();
            modelBuilder.Entity<CollectionJoin>().HasOne(x => x.Author).WithMany(x => x.CollectionJoins).IsRequired();
        }
    }
}
