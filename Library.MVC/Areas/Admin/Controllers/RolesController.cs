using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Library.MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: /Admin/Roles
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            Log.Information("Roles index loaded. Total roles: {Count}", roles.Count);
            return View(roles);
        }

        // POST: /Admin/Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                Log.Warning("Attempt to create role with empty name");
                TempData["Error"] = "Role name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
                if (result.Succeeded)
                {
                    Log.Information("Role created: {RoleName}", roleName.Trim());
                    TempData["Success"] = $"Role '{roleName.Trim()}' created successfully.";
                }
                else
                {
                    Log.Error("Failed to create role: {RoleName}. Errors: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["Error"] = "Failed to create role.";
                }
            }
            else
            {
                Log.Warning("Attempt to create duplicate role: {RoleName}", roleName);
                TempData["Error"] = "Role already exists.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Roles/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Log.Warning("Attempt to delete role with empty ID");
                TempData["Error"] = "Invalid role ID.";
                return RedirectToAction(nameof(Index));
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    Log.Information("Role deleted: {RoleName} (ID: {RoleId})", role.Name, role.Id);
                    TempData["Success"] = $"Role '{role.Name}' deleted successfully.";
                }
                else
                {
                    Log.Error("Failed to delete role: {RoleName}. Errors: {Errors}", role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["Error"] = "Failed to delete role.";
                }
            }
            else
            {
                Log.Warning("Attempt to delete non-existent role: {RoleId}", id);
                TempData["Error"] = "Role not found.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}