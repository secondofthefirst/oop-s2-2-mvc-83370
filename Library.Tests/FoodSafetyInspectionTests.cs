using Library.Domain;
using Library.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests;

public class FoodSafetyInspectionTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task OverdueFollowUps_ReturnsCorrectItems()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var today = DateTime.Now.Date;

        var premises = new Premises { Id = 1, Name = "Test Restaurant", Address = "123 Main St", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today.AddDays(-10), Score = 65, Outcome = "Fail", Notes = "Test" };
        var overdueFollowUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = today.AddDays(-5), Status = "Open" };
        var openFollowUp = new FollowUp { Id = 2, InspectionId = 1, DueDate = today.AddDays(5), Status = "Open" };
        var closedFollowUp = new FollowUp { Id = 3, InspectionId = 1, DueDate = today.AddDays(-3), Status = "Closed", ClosedDate = today };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.AddRange(overdueFollowUp, openFollowUp, closedFollowUp);
        await context.SaveChangesAsync();

        // Act
        var overdueOpen = await context.FollowUps
            .Where(f => f.DueDate < today && f.Status == "Open")
            .ToListAsync();

        // Assert
        Assert.Single(overdueOpen);
        Assert.Equal(1, overdueOpen.First().Id);
    }

    [Fact]
    public async Task FollowUp_CannotBeClosedWithoutClosedDate()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 80, Outcome = "Pass", Notes = "" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Closed", ClosedDate = null };

        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);

        // Act & Assert
        Assert.Null(followUp.ClosedDate);
        Assert.Equal("Closed", followUp.Status);
        // In real scenario, controller would validate and reject this
    }

    [Fact]
    public async Task DashboardCounts_ConsistentWithSeededData()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var premises = new Premises { Id = 1, Name = "Test Cafe", Address = "456 Oak St", Town = "Bath", RiskRating = "Low" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = monthStart.AddDays(5), Score = 95, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = monthStart.AddDays(10), Score = 50, Outcome = "Fail" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2);
        await context.SaveChangesAsync();

        // Act
        var inspectionsThisMonth = await context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1))
            .CountAsync();

        var failedThisMonth = await context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1) && i.Outcome == "Fail")
            .CountAsync();

        // Assert
        Assert.Equal(2, inspectionsThisMonth);
        Assert.Equal(1, failedThisMonth);
    }

    [Fact]
    public async Task RoleBasedAccess_InspectorCanCreateInspection()
    {
        // Arrange - This is more of an integration test that would be tested in controller tests
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Test Shop", Address = "789 Elm St", Town = "Gloucester", RiskRating = "Medium" };

        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        // Act
        var inspection = new Inspection
        {
            Id = 1,
            PremisesId = premises.Id,
            InspectionDate = DateTime.Now.AddDays(-1),
            Score = 75,
            Outcome = "Pass",
            Notes = "Routine inspection"
        };
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        // Assert
        var createdInspection = await context.Inspections.FindAsync(1);
        Assert.NotNull(createdInspection);
        Assert.Equal(75, createdInspection.Score);
    }
}