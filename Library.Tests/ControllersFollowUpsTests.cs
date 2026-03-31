using Library.Domain;
using Library.MVC.Controllers;
using Library.MVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests;

public class FollowUpsControllerTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Index_ReturnsAllFollowUps()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123 Main", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-10), Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp1 = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };
        var followUp2 = new FollowUp { Id = 2, InspectionId = 1, DueDate = DateTime.Now.AddDays(-5), Status = "Closed", ClosedDate = DateTime.Now };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.AddRange(followUp1, followUp2);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedFollowUps = Assert.IsType<List<FollowUp>>(viewResult.Model);
        Assert.Equal(2, returnedFollowUps.Count);
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123 Main", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedFollowUp = Assert.IsType<FollowUp>(viewResult.Model);
        Assert.Equal(1, returnedFollowUp.Id);
        Assert.Equal("Open", returnedFollowUp.Status);
    }

    [Fact]
    public async Task Details_WithNullId_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Details(null);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_ReturnsInspectionsList()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection1 = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 50, Outcome = "Fail", Notes = "Test" };
        var inspection2 = new Inspection { Id = 2, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-5), Score = 85, Outcome = "Pass", Notes = "Good" };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspection1, inspection2);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Create();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var inspectionsInViewData = (List<Inspection>)viewResult.ViewData["Inspections"];
        Assert.NotNull(inspectionsInViewData);
        Assert.Equal(2, inspectionsInViewData.Count);
    }

    [Fact]
    public async Task Create_Post_WithValidModel_SavesFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-10), Score = 50, Outcome = "Fail", Notes = "Test" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        // Act
        var result = await controller.Create(followUp);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(FollowUpsController.Index), redirectResult.ActionName);

        var savedFollowUp = await context.FollowUps.FindAsync(1);
        Assert.NotNull(savedFollowUp);
        Assert.Equal("Open", savedFollowUp.Status);
    }

    [Fact]
    public async Task Create_Post_WithNonExistentInspection_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);
        var followUp = new FollowUp { Id = 1, InspectionId = 999, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        // Act
        var result = await controller.Create(followUp);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("InspectionId"));
    }

    [Fact]
    public async Task Create_Post_WithDueDateBeforeInspectionDate_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspectionDate = DateTime.Now.AddDays(-10);
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = inspectionDate, Score = 50, Outcome = "Fail", Notes = "Test" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = inspectionDate.AddDays(-5), Status = "Open", ClosedDate = null };

        // Act
        var result = await controller.Create(followUp);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("DueDate"));
    }

    [Fact]
    public async Task Create_Post_WithClosedStatusWithoutClosedDate_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-10), Score = 50, Outcome = "Fail", Notes = "Test" };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Closed", ClosedDate = null };

        // Act
        var result = await controller.Create(followUp);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("ClosedDate"));
    }

    [Fact]
    public async Task Edit_Get_WithValidId_ReturnsFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedFollowUp = Assert.IsType<FollowUp>(viewResult.Model);
        Assert.Equal(1, returnedFollowUp.Id);
    }

    [Fact]
    public async Task Edit_Get_WithNullId_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Edit(null);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_WithValidModel_UpdatesFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-10), Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);
        // Get a fresh instance from the database to avoid tracking conflicts
        var followUpToUpdate = await context.FollowUps.FindAsync(1);
        followUpToUpdate.DueDate = DateTime.Now.AddDays(10);
        followUpToUpdate.Status = "Closed";
        followUpToUpdate.ClosedDate = DateTime.Now;

        // Act
        var result = await controller.Edit(1, followUpToUpdate);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(FollowUpsController.Index), redirectResult.ActionName);

        var saved = await context.FollowUps.FindAsync(1);
        Assert.Equal("Closed", saved.Status);
        Assert.NotNull(saved.ClosedDate);
    }

    [Fact]
    public async Task Edit_Post_WithClosedStatusWithoutClosedDate_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now.AddDays(-10), Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);
        var updatedFollowUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(10), Status = "Closed", ClosedDate = null };

        // Act
        var result = await controller.Edit(1, updatedFollowUp);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("ClosedDate"));
    }

    [Fact]
    public async Task Edit_Post_WithMismatchedId_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now, Status = "Open", ClosedDate = null };

        // Act
        var result = await controller.Edit(999, followUp);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_WithValidId_ReturnsFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedFollowUp = Assert.IsType<FollowUp>(viewResult.Model);
        Assert.Equal(1, returnedFollowUp.Id);
    }

    [Fact]
    public async Task Delete_Post_DeletesFollowUp()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var premises = new Premises { Id = 1, Name = "Restaurant A", Address = "123", Town = "Bristol", RiskRating = "High" };
        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.Now, Score = 50, Outcome = "Fail", Notes = "Test" };
        var followUp = new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.Now.AddDays(5), Status = "Open", ClosedDate = null };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(FollowUpsController.Index), redirectResult.ActionName);

        var deleted = await context.FollowUps.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Delete_Post_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var controller = new FollowUpsController(context);

        // Act
        var result = await controller.DeleteConfirmed(999);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(FollowUpsController.Index), redirectResult.ActionName);
    }
}
