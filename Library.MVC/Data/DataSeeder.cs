using Bogus;
using Library.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Library.MVC.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (context.Premises.Any())
            {
                // Already seeded
                return;
            }

            // Seed Premises (12 across 3 towns)
            var towns = new[] { "Bristol", "Bath", "Gloucester" };
            var riskRatings = new[] { "Low", "Medium", "High" };
            var premisesFaker = new Faker<Premises>()
                .RuleFor(p => p.Name, f => $"{f.Company.CompanyName()} {f.Random.Word()}")
                .RuleFor(p => p.Address, f => f.Address.StreetAddress())
                .RuleFor(p => p.Town, f => f.PickRandom(towns))
                .RuleFor(p => p.RiskRating, f => f.PickRandom(riskRatings));

            var premises = premisesFaker.Generate(12);
            await context.Premises.AddRangeAsync(premises);
            await context.SaveChangesAsync();

            // Seed Inspections (25 across different dates)
            var inspectionFaker = new Faker<Inspection>()
                .RuleFor(i => i.PremisesId, f => f.PickRandom(premises).Id)
                .RuleFor(i => i.InspectionDate, f => f.Date.Between(DateTime.Now.AddMonths(-3), DateTime.Now))
                .RuleFor(i => i.Score, f => f.Random.Int(0, 100))
                .RuleFor(i => i.Outcome, (f, i) => i.Score >= 70 ? "Pass" : "Fail")
                .RuleFor(i => i.Notes, f => f.Lorem.Sentence());

            var inspections = inspectionFaker.Generate(25);
            await context.Inspections.AddRangeAsync(inspections);
            await context.SaveChangesAsync();

            var followUpFaker = new Faker<FollowUp>()
                .RuleFor(f => f.InspectionId, f => f.PickRandom(inspections.Take(10)).Id)
                .RuleFor(f => f.DueDate, (f, fu) =>
                {
                    var inspection = inspections.First(i => i.Id == fu.InspectionId);
                    // Some overdue, some future
                    return f.PickRandom(
                        inspection.InspectionDate.AddDays(f.Random.Int(-30, -1)), // overdue
                        inspection.InspectionDate.AddDays(f.Random.Int(5, 60)) // future
                    );
                })
                .RuleFor(f => f.Status, f => f.PickRandom("Open", "Closed"))
                .RuleFor(f => f.ClosedDate, (f, fu) => fu.Status == "Closed" ? DateTime.Now.AddDays(f.Random.Int(-20, -1)) : null);

            var followUps = followUpFaker.Generate(10);
            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }
    }
}
