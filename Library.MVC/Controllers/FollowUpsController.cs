using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.MVC.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FollowUpsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: FollowUps
    public async Task<IActionResult> Index()
    {
        var followUps = await _context.FollowUps
            .Include(f => f.Inspection)
            .ThenInclude(i => i.Premises)
            .ToListAsync();

        Log.Information("Follow-ups list viewed. Total: {Count}", followUps.Count);
        return View(followUps);
    }

    // GET: FollowUps/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var followUp = await _context.FollowUps
            .Include(f => f.Inspection)
            .ThenInclude(i => i.Premises)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (followUp == null) return NotFound();

        return View(followUp);
    }

    // GET: FollowUps/Create
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Inspections = await _context.Inspections
            .Include(i => i.Premises)
            .ToListAsync();
        return View();
    }

    // POST: FollowUps/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create([Bind("Id,InspectionId,DueDate,Status,ClosedDate")] FollowUp followUp)
    {
        var inspection = await _context.Inspections.FindAsync(followUp.InspectionId);
        if (inspection == null)
        {
            ModelState.AddModelError("InspectionId", "Inspection not found");
            Log.Warning("Attempt to create follow-up for non-existent inspection: {InspectionId}", followUp.InspectionId);
        }
        else if (followUp.DueDate < inspection.InspectionDate)
        {
            ModelState.AddModelError("DueDate", "Follow-up due date cannot be before inspection date");
            Log.Warning("Invalid follow-up due date: Due={DueDate}, InspectionDate={InspectionDate}",
                followUp.DueDate, inspection.InspectionDate);
        }

        if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
        {
            ModelState.AddModelError("ClosedDate", "ClosedDate is required when closing a follow-up");
            Log.Warning("Attempt to create closed follow-up without ClosedDate");
        }

        if (ModelState.IsValid)
        {
            _context.Add(followUp);
            await _context.SaveChangesAsync();
            Log.Information("Follow-up created: FollowUpId={Id}, InspectionId={InspectionId}, DueDate={DueDate}, Status={Status}",
                followUp.Id, followUp.InspectionId, followUp.DueDate, followUp.Status);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Inspections = await _context.Inspections
            .Include(i => i.Premises)
            .ToListAsync();
        return View(followUp);
    }

    // GET: FollowUps/Edit/5
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        ViewBag.Inspections = await _context.Inspections
            .Include(i => i.Premises)
            .ToListAsync();
        return View(followUp);
    }

    // POST: FollowUps/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionId,DueDate,Status,ClosedDate")] FollowUp followUp)
    {
        if (id != followUp.Id) return NotFound();

        if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
        {
            ModelState.AddModelError("ClosedDate", "ClosedDate is required when closing a follow-up");
            Log.Warning("Attempt to close follow-up without ClosedDate: FollowUpId={Id}", id);
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(followUp);
                await _context.SaveChangesAsync();
                Log.Information("Follow-up updated: FollowUpId={Id}, Status={Status}, ClosedDate={ClosedDate}",
                    followUp.Id, followUp.Status, followUp.ClosedDate);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FollowUpExists(followUp.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Inspections = await _context.Inspections
            .Include(i => i.Premises)
            .ToListAsync();
        return View(followUp);
    }

    // GET: FollowUps/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var followUp = await _context.FollowUps
            .Include(f => f.Inspection)
            .ThenInclude(i => i.Premises)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (followUp == null) return NotFound();

        return View(followUp);
    }

    // POST: FollowUps/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();
            Log.Information("Follow-up deleted: FollowUpId={Id}", id);
        }
        return RedirectToAction(nameof(Index));
    }

    private bool FollowUpExists(int id) => _context.FollowUps.Any(e => e.Id == id);
}
