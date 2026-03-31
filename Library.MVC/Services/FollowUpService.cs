using Library.Domain;
using Library.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Services;

public class FollowUpService
{
    private readonly ApplicationDbContext _context;

    public FollowUpService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all follow-ups that are overdue and still open.
    /// </summary>
    public async Task<List<FollowUp>> GetOverdueOpenFollowUpsAsync()
    {
        var today = DateTime.Now.Date;
        return await _context.FollowUps
            .Where(f => f.DueDate < today && f.Status == "Open")
            .Include(f => f.Inspection)
            .ThenInclude(i => i.Premises)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all follow-ups for a specific inspection.
    /// </summary>
    public async Task<List<FollowUp>> GetFollowUpsForInspectionAsync(int inspectionId)
    {
        return await _context.FollowUps
            .Where(f => f.InspectionId == inspectionId)
            .Include(f => f.Inspection)
            .ToListAsync();
    }

    /// <summary>
    /// Validates and creates a new follow-up.
    /// Returns a tuple of (success, errorMessage).
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> CreateFollowUpAsync(FollowUp followUp)
    {
        // Validate inspection exists
        var inspection = await _context.Inspections.FindAsync(followUp.InspectionId);
        if (inspection == null)
        {
            return (false, "Inspection not found");
        }

        // Validate due date is not before inspection date
        if (followUp.DueDate < inspection.InspectionDate)
        {
            return (false, "Follow-up due date cannot be before inspection date");
        }

        // Validate closed follow-ups must have a closed date
        if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
        {
            return (false, "ClosedDate is required when closing a follow-up");
        }

        // Validate closed date is not in the future
        if (followUp.ClosedDate.HasValue && followUp.ClosedDate.Value.Date > DateTime.Now.Date)
        {
            return (false, "Closed date cannot be in the future");
        }

        _context.Add(followUp);
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    /// <summary>
    /// Validates and updates an existing follow-up.
    /// Returns a tuple of (success, errorMessage).
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> UpdateFollowUpAsync(int id, FollowUp followUp)
    {
        var existingFollowUp = await _context.FollowUps.FindAsync(id);
        if (existingFollowUp == null)
        {
            return (false, "Follow-up not found");
        }

        var inspection = await _context.Inspections.FindAsync(followUp.InspectionId);
        if (inspection == null)
        {
            return (false, "Inspection not found");
        }

        if (followUp.DueDate < inspection.InspectionDate)
        {
            return (false, "Follow-up due date cannot be before inspection date");
        }

        if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
        {
            return (false, "ClosedDate is required when closing a follow-up");
        }

        if (followUp.ClosedDate.HasValue && followUp.ClosedDate.Value.Date > DateTime.Now.Date)
        {
            return (false, "Closed date cannot be in the future");
        }

        existingFollowUp.InspectionId = followUp.InspectionId;
        existingFollowUp.DueDate = followUp.DueDate;
        existingFollowUp.Status = followUp.Status;
        existingFollowUp.ClosedDate = followUp.ClosedDate;

        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    /// <summary>
    /// Closes a follow-up with the current date.
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> CloseFollowUpAsync(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return (false, "Follow-up not found");
        }

        if (followUp.Status == "Closed")
        {
            return (false, "Follow-up is already closed");
        }

        followUp.Status = "Closed";
        followUp.ClosedDate = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    /// <summary>
    /// Gets count of open follow-ups for a specific premises.
    /// </summary>
    public async Task<int> GetOpenFollowUpsCountForPremisesAsync(int premisesId)
    {
        return await _context.FollowUps
            .Where(f => f.Inspection.Premises.Id == premisesId && f.Status == "Open")
            .CountAsync();
    }
}
