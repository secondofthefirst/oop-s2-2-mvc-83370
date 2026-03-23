using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.MVC.Controllers;

[Authorize]
public class PremisesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PremisesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Premises
    public async Task<IActionResult> Index()
    {
        var premises = await _context.Premises.Include(p => p.Inspections).ToListAsync();
        Log.Information("Premises list viewed. Total: {Count}", premises.Count);
        return View(premises);
    }

    // GET: Premises/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var premises = await _context.Premises
            .Include(p => p.Inspections)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (premises == null) return NotFound();

        return View(premises);
    }

    // GET: Premises/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Premises/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Id,Name,Address,Town,RiskRating")] Premises premises)
    {
        if (ModelState.IsValid)
        {
            _context.Add(premises);
            await _context.SaveChangesAsync();
            Log.Information("Premises created: PremisesId={Id}, Name={Name}, Town={Town}", premises.Id, premises.Name, premises.Town);
            return RedirectToAction(nameof(Index));
        }
        return View(premises);
    }

    // GET: Premises/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var premises = await _context.Premises.FindAsync(id);
        if (premises == null) return NotFound();

        return View(premises);
    }

    // POST: Premises/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Town,RiskRating")] Premises premises)
    {
        if (id != premises.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(premises);
                await _context.SaveChangesAsync();
                Log.Information("Premises updated: PremisesId={Id}, RiskRating={Rating}", premises.Id, premises.RiskRating);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PremisesExists(premises.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(premises);
    }

    // GET: Premises/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var premises = await _context.Premises
            .FirstOrDefaultAsync(m => m.Id == id);
        if (premises == null) return NotFound();

        return View(premises);
    }

    // POST: Premises/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var premises = await _context.Premises.FindAsync(id);
        if (premises != null)
        {
            _context.Premises.Remove(premises);
            await _context.SaveChangesAsync();
            Log.Information("Premises deleted: PremisesId={Id}", id);
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PremisesExists(int id) => _context.Premises.Any(e => e.Id == id);
}
