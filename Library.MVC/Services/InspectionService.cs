using Library.Domain;
using Library.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Services;

public class InspectionService
{
    private readonly ApplicationDbContext _context;

    public InspectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all inspections for a premises.
    /// </summary>
    public async Task<List<Inspection>> GetInspectionsForPremisesAsync(int premisesId)
    {
        return await _context.Inspections
            .Where(i => i.PremisesId == premisesId)
            .Include(i => i.FollowUps)
            .OrderByDescending(i => i.InspectionDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets inspections for the current month.
    /// </summary>
    public async Task<List<Inspection>> GetInspectionsForCurrentMonthAsync()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return await _context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1))
            .Include(i => i.Premises)
            .Include(i => i.FollowUps)
            .ToListAsync();
    }

    /// <summary>
    /// Gets count of inspections for the current month.
    /// </summary>
    public async Task<int> GetInspectionsCountForCurrentMonthAsync()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return await _context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1))
            .CountAsync();
    }

    /// <summary>
    /// Gets count of failed inspections for the current month.
    /// </summary>
    public async Task<int> GetFailedInspectionsCountForCurrentMonthAsync()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return await _context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1) && i.Outcome == "Fail")
            .CountAsync();
    }

    /// <summary>
    /// Gets count of passed inspections for the current month.
    /// </summary>
    public async Task<int> GetPassedInspectionsCountForCurrentMonthAsync()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return await _context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1) && i.Outcome == "Pass")
            .CountAsync();
    }

    /// <summary>
    /// Validates and creates a new inspection.
    /// Returns a tuple of (success, errorMessage).
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> CreateInspectionAsync(Inspection inspection)
    {
        // Validate premises exists
        var premises = await _context.Premises.FindAsync(inspection.PremisesId);
        if (premises == null)
        {
            return (false, "Premises not found");
        }

        // Validate inspection date is not in the future
        if (inspection.InspectionDate.Date > DateTime.Now.Date)
        {
            return (false, "Inspection date cannot be in the future");
        }

        // Validate score is in valid range
        if (inspection.Score < 0 || inspection.Score > 100)
        {
            return (false, "Score must be between 0 and 100");
        }

        // Validate outcome
        var validOutcomes = new[] { "Pass", "Fail" };
        if (!validOutcomes.Contains(inspection.Outcome))
        {
            return (false, "Outcome must be 'Pass' or 'Fail'");
        }

        _context.Add(inspection);
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    /// <summary>
    /// Validates and updates an existing inspection.
    /// Returns a tuple of (success, errorMessage).
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> UpdateInspectionAsync(int id, Inspection inspection)
    {
        var existingInspection = await _context.Inspections.FindAsync(id);
        if (existingInspection == null)
        {
            return (false, "Inspection not found");
        }

        var premises = await _context.Premises.FindAsync(inspection.PremisesId);
        if (premises == null)
        {
            return (false, "Premises not found");
        }

        if (inspection.InspectionDate.Date > DateTime.Now.Date)
        {
            return (false, "Inspection date cannot be in the future");
        }

        if (inspection.Score < 0 || inspection.Score > 100)
        {
            return (false, "Score must be between 0 and 100");
        }

        var validOutcomes = new[] { "Pass", "Fail" };
        if (!validOutcomes.Contains(inspection.Outcome))
        {
            return (false, "Outcome must be 'Pass' or 'Fail'");
        }

        existingInspection.PremisesId = inspection.PremisesId;
        existingInspection.InspectionDate = inspection.InspectionDate;
        existingInspection.Score = inspection.Score;
        existingInspection.Outcome = inspection.Outcome;
        existingInspection.Notes = inspection.Notes;

        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    /// <summary>
    /// Gets the average score for a premises across all inspections.
    /// </summary>
    public async Task<double> GetAverageScoreForPremisesAsync(int premisesId)
    {
        var inspections = await _context.Inspections
            .Where(i => i.PremisesId == premisesId)
            .ToListAsync();

        if (inspections.Count == 0)
            return 0;

        return inspections.Average(i => i.Score);
    }

    /// <summary>
    /// Gets the most recent inspection for a premises.
    /// </summary>
    public async Task<Inspection?> GetMostRecentInspectionForPremisesAsync(int premisesId)
    {
        return await _context.Inspections
            .Where(i => i.PremisesId == premisesId)
            .OrderByDescending(i => i.InspectionDate)
            .FirstOrDefaultAsync();
    }
}
