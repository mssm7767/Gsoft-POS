using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Controllers
{
    public class StationInformationController : Controller
    {
        private readonly AppDbContext _context;

        public StationInformationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stations = await _context.TerminalPrinterSettings
                .OrderBy(x => x.Id)
                .ToListAsync();

            return View(stations);
        }

        public IActionResult Create()
        {
            return View(new TerminalPrinterSetting());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TerminalPrinterSetting model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.TerminalPrinterSettings.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var station = await _context.TerminalPrinterSettings.FindAsync(id);
            if (station == null)
                return NotFound();

            return View(station);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TerminalPrinterSetting model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.TerminalPrinterSettings.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var station = await _context.TerminalPrinterSettings.FindAsync(id);
            if (station == null)
                return NotFound();

            _context.TerminalPrinterSettings.Remove(station);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}