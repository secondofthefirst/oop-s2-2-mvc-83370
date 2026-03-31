using Library.Domain;
using Library.MVC.Data;
using Library.MVC.Services;
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

    #region FollowUpService Tests

    [Fact]
    public async Task FollowUpService_GetOverdueOpenFollowUpsAsync_ReturnsCorrectItems()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
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
        var result = await service.GetOverdueOpenFollowUpsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result.First().Id);
        Assert.Equal("Open", result.First().Status);
        Assert.True(result.First().DueDate < today);
    }

    [Fact]
    public async Task FollowUpService_CreateFollowUpAsync_FailsWithoutClosedDate()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 80, Outcome = "Pass", Notes = "" };
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Closed", ClosedDate = null };

        // Act
        var (success, errorMessage) = await service.CreateFollowUpAsync(followUp);

        // Assert
        Assert.False(success);
        Assert.Equal("ClosedDate is required when closing a follow-up", errorMessage);
    }

    [Fact]
    public async Task FollowUpService_CreateFollowUpAsync_FailsWhenDueDateBeforeInspectionDate()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
        var inspectionDate = DateTime.Now.Date;
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = inspectionDate, Score = 80, Outcome = "Pass", Notes = "" };
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = inspectionDate.AddDays(-1), Status = "Open" };

        // Act
        var (success, errorMessage) = await service.CreateFollowUpAsync(followUp);

        // Assert
        Assert.False(success);
        Assert.Equal("Follow-up due date cannot be before inspection date", errorMessage);
    }

    [Fact]
    public async Task FollowUpService_CreateFollowUpAsync_SucceedsWithValidData()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
        var today = DateTime.Now.Date;
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today, Score = 80, Outcome = "Pass", Notes = "" };
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = today.AddDays(5), Status = "Open" };

        // Act
        var (success, errorMessage) = await service.CreateFollowUpAsync(followUp);

        // Assert
        Assert.True(success);
        Assert.Empty(errorMessage);
        var createdFollowUp = await context.FollowUps.FindAsync(1);
        Assert.NotNull(createdFollowUp);
    }

    [Fact]
    public async Task FollowUpService_CloseFollowUpAsync_SucceedsAndSetsDates()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
        var today = DateTime.Now.Date;
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today, Score = 80, Outcome = "Pass", Notes = "" };
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = today.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await service.CloseFollowUpAsync(1);

        // Assert
        Assert.True(success);
        var updatedFollowUp = await context.FollowUps.FindAsync(1);
        Assert.NotNull(updatedFollowUp);
        Assert.Equal("Closed", updatedFollowUp.Status);
        Assert.NotNull(updatedFollowUp.ClosedDate);
    }

    [Fact]
    public async Task FollowUpService_GetOpenFollowUpsCountForPremisesAsync_CountsCorrectly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new FollowUpService(context);
        var today = DateTime.Now.Date;
        var premises = new Premises { Id = 1, Name = "Test Restaurant", Address = "123 Main St", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today.AddDays(-10), Score = 65, Outcome = "Fail", Notes = "Test" };
        var openFollowUp1 = new FollowUp { Id = 1, InspectionId = 1, DueDate = today.AddDays(5), Status = "Open" };
        var openFollowUp2 = new FollowUp { Id = 2, InspectionId = 1, DueDate = today.AddDays(10), Status = "Open" };
        var closedFollowUp = new FollowUp { Id = 3, InspectionId = 1, DueDate = today.AddDays(-3), Status = "Closed", ClosedDate = today };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.AddRange(openFollowUp1, openFollowUp2, closedFollowUp);
        await context.SaveChangesAsync();

        // Act
        var count = await service.GetOpenFollowUpsCountForPremisesAsync(1);

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region InspectionService Tests

    [Fact]
    public async Task InspectionService_GetInspectionsForCurrentMonthAsync_ReturnCorrectCount()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var premises = new Premises { Id = 1, Name = "Test Cafe", Address = "456 Oak St", Town = "Bath", RiskRating = "Low" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = monthStart.AddDays(5), Score = 95, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = monthStart.AddDays(10), Score = 50, Outcome = "Fail" };
        var lastMonthInspection = new Inspection { Id = 3, PremisesId = 1, InspectionDate = monthStart.AddDays(-5), Score = 70, Outcome = "Pass" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2, lastMonthInspection);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetInspectionsForCurrentMonthAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task InspectionService_GetInspectionsCountForCurrentMonthAsync_ReturnsCorrectCount()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var premises = new Premises { Id = 1, Name = "Test Cafe", Address = "456 Oak St", Town = "Bath", RiskRating = "Low" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = monthStart.AddDays(5), Score = 95, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = monthStart.AddDays(10), Score = 50, Outcome = "Fail" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2);
        await context.SaveChangesAsync();

        // Act
        var inspectionsThisMonth = await service.GetInspectionsCountForCurrentMonthAsync();

        // Assert
        Assert.Equal(2, inspectionsThisMonth);
    }

    [Fact]
    public async Task InspectionService_GetFailedInspectionsCountForCurrentMonthAsync_ReturnsCorrectCount()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var premises = new Premises { Id = 1, Name = "Test Cafe", Address = "456 Oak St", Town = "Bath", RiskRating = "Low" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = monthStart.AddDays(5), Score = 95, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = monthStart.AddDays(10), Score = 50, Outcome = "Fail" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2);
        await context.SaveChangesAsync();

        // Act
        var failedThisMonth = await service.GetFailedInspectionsCountForCurrentMonthAsync();

        // Assert
        Assert.Equal(1, failedThisMonth);
    }

    [Fact]
    public async Task InspectionService_CreateInspectionAsync_FailsWithFutureDate()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(1), Score = 75, Outcome = "Pass", Notes = "" };

        // Act
        var (success, errorMessage) = await service.CreateInspectionAsync(inspection);

        // Assert
        Assert.False(success);
        Assert.Equal("Inspection date cannot be in the future", errorMessage);
    }

    [Fact]
    public async Task InspectionService_CreateInspectionAsync_FailsWithInvalidScore()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 150, Outcome = "Pass", Notes = "" };

        // Act
        var (success, errorMessage) = await service.CreateInspectionAsync(inspection);

        // Assert
        Assert.False(success);
        Assert.Equal("Score must be between 0 and 100", errorMessage);
    }

    [Fact]
    public async Task InspectionService_CreateInspectionAsync_FailsWithInvalidOutcome()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };

        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 75, Outcome = "Invalid", Notes = "" };

        // Act
        var (success, errorMessage) = await service.CreateInspectionAsync(inspection);

        // Assert
        Assert.False(success);
        Assert.Equal("Outcome must be 'Pass' or 'Fail'", errorMessage);
    }

    [Fact]
    public async Task InspectionService_CreateInspectionAsync_SucceedsWithValidData()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var premises = new Premises { Id = 1, Name = "Test Shop", Address = "789 Elm St", Town = "Gloucester", RiskRating = "Medium" };

        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-1), Score = 75, Outcome = "Pass", Notes = "Routine inspection" };

        // Act
        var (success, errorMessage) = await service.CreateInspectionAsync(inspection);

        // Assert
        Assert.True(success);
        Assert.Empty(errorMessage);
        var createdInspection = await context.Inspections.FindAsync(1);
        Assert.NotNull(createdInspection);
        Assert.Equal(75, createdInspection.Score);
    }

    [Fact]
    public async Task InspectionService_GetAverageScoreForPremisesAsync_CalculatesCorrectly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var today = DateTime.Now.Date;
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today.AddDays(-30), Score = 80, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = today.AddDays(-20), Score = 90, Outcome = "Pass" };
        var inspection3 = new Inspection { Id = 3, PremisesId = 1, InspectionDate = today.AddDays(-10), Score = 70, Outcome = "Fail" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2, inspection3);
        await context.SaveChangesAsync();

        // Act
        var average = await service.GetAverageScoreForPremisesAsync(1);

        // Assert
        Assert.Equal(80, average);
    }

    [Fact]
    public async Task InspectionService_GetMostRecentInspectionForPremisesAsync_ReturnsMostRecent()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new InspectionService(context);
        var today = DateTime.Now.Date;
        var premises = new Premises { Id = 1, Name = "Test", Address = "Test", Town = "Test", RiskRating = "Medium" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = today.AddDays(-30), Score = 80, Outcome = "Pass" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = today.AddDays(-10), Score = 90, Outcome = "Pass" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2);
        await context.SaveChangesAsync();

        // Act
        var mostRecent = await service.GetMostRecentInspectionForPremisesAsync(1);

        // Assert
        Assert.NotNull(mostRecent);
        Assert.Equal(2, mostRecent.Id);
        Assert.Equal(90, mostRecent.Score);
    }

    #endregion
}