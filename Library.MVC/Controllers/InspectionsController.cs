using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.MVC.Controllers;

[Authorize]
public class InspectionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public InspectionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Inspections
    public async Task<IActionResult> Index()
    {
        var inspections = await _context.Inspections
            .Include(i => i.Premises)
            .Include(i => i.FollowUps)
            .ToListAsync();

        Log.Information("Inspections list viewed. Total: {Count}", inspections.Count);
        return View(inspections);
    }

    // GET: Inspections/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var inspection = await _context.Inspections
            .Include(i => i.Premises)
            .Include(i => i.FollowUps)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (inspection == null) return NotFound();

        return View(inspection);
    }

    // GET: Inspections/Create
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Premises = await _context.Premises.ToListAsync();
        return View();
    }

    // POST: Inspections/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create([Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
    {
        if (inspection.InspectionDate > DateTime.Now)
        {
            ModelState.AddModelError("InspectionDate", "Inspection date cannot be in the future");
            Log.Warning("Invalid inspection date attempted: {Date}", inspection.InspectionDate);
        }

        if (ModelState.IsValid)
        {
            _context.Add(inspection);
            await _context.SaveChangesAsync();
            Log.Information("Inspection created: InspectionId={Id}, PremisesId={PremisesId}, Score={Score}, Outcome={Outcome}",
                inspection.Id, inspection.PremisesId, inspection.Score, inspection.Outcome);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Premises = await _context.Premises.ToListAsync();
        return View(inspection);
    }

    // GET: Inspections/Edit/5
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var inspection = await _context.Inspections.FindAsync(id);
        if (inspection == null) return NotFound();

        ViewBag.Premises = await _context.Premises.ToListAsync();
        return View(inspection);
    }

    // POST: Inspections/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
    {
        if (id != inspection.Id) return NotFound();

        if (inspection.InspectionDate > DateTime.Now)
        {
            ModelState.AddModelError("InspectionDate", "Inspection date cannot be in the future");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(inspection);
                await _context.SaveChangesAsync();
                Log.Information("Inspection updated: InspectionId={Id}, Outcome={Outcome}", inspection.Id, inspection.Outcome);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InspectionExists(inspection.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Premises = await _context.Premises.ToListAsync();
        return View(inspection);
    }

    // GET: Inspections/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var inspection = await _context.Inspections
            .Include(i => i.Premises)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (inspection == null) return NotFound();

        return View(inspection);
    }

    // POST: Inspections/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inspection = await _context.Inspections.FindAsync(id);
        if (inspection != null)
        {
            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync();
            Log.Information("Inspection deleted: InspectionId={Id}", id);
        }
        return RedirectToAction(nameof(Index));
    }

    private bool InspectionExists(int id) => _context.Inspections.Any(e => e.Id == id);
}
