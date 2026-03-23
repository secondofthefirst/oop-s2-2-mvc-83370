using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.MVC.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardModel> _logger;

    public int InspectionsThisMonth { get; set; }
    public int FailedInspectionsThisMonth { get; set; }
    public int OverdueFollowUps { get; set; }
    public List<Premises> FilteredPremises { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SelectedTown { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedRiskRating { get; set; }

    public List<string> AvailableTowns { get; set; } = new();
    public List<string> AvailableRiskRatings { get; set; } = new() { "Low", "Medium", "High" };

    public DashboardModel(ApplicationDbContext context, ILogger<DashboardModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var today = DateTime.Now.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // Count inspections this month
            InspectionsThisMonth = await _context.Inspections
                .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1))
                .CountAsync();

            // Count failed inspections this month
            FailedInspectionsThisMonth = await _context.Inspections
                .Where(i => i.InspectionDate >= monthStart && i.InspectionDate <= today.AddDays(1) && i.Outcome == "Fail")
                .CountAsync();

            // Count overdue follow-ups (DueDate < Today AND Status = Open)
            OverdueFollowUps = await _context.FollowUps
                .Where(f => f.DueDate < today && f.Status == "Open")
                .CountAsync();

            // Get available towns for filter dropdown
            AvailableTowns = await _context.Premises
                .Select(p => p.Town)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            // Apply filters
            var query = _context.Premises.AsQueryable();

            if (!string.IsNullOrEmpty(SelectedTown))
            {
                query = query.Where(p => p.Town == SelectedTown);
                Log.Information("Dashboard filtered by Town: {Town}", SelectedTown);
            }

            if (!string.IsNullOrEmpty(SelectedRiskRating))
            {
                query = query.Where(p => p.RiskRating == SelectedRiskRating);
                Log.Information("Dashboard filtered by RiskRating: {RiskRating}", SelectedRiskRating);
            }

            FilteredPremises = await query
                .Include(p => p.Inspections)
                .ToListAsync();

            Log.Information("Dashboard loaded - Month: {Month}, Inspections: {Count}, Failed: {Failed}, Overdue: {Overdue}",
                today.Month, InspectionsThisMonth, FailedInspectionsThisMonth, OverdueFollowUps);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading dashboard");
            throw;
        }
    }
}
