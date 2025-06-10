using System.Data.Entity.Migrations;

namespace FerryBookingSystem.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<FerryBookingSystem.Models.FerryBookingContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "FerryBookingSystem.Models.FerryBookingContext";
        }

        protected override void Seed(FerryBookingSystem.Models.FerryBookingContext context)
        {
            // This method will be called after migrating to the latest version.
            // You can use the DbSet<T>.AddOrUpdate() helper extension method
            // to avoid creating duplicate seed data.

            // Ensure the database is created and up to date
            context.Database.CreateIfNotExists();
        }
    }
}
