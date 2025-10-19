using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GSoftPosNew.Controllers
{
    public class StockAdjustmentController : Controller
    {
        private readonly AppDbContext _context;

        public StockAdjustmentController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string refNo = "")
        {
            ViewBag.UserName = User.Identity?.Name ?? "Unknown";
            ViewBag.Items = await _context.Items.ToListAsync();

            var stockAdjustment = new StockAdjustment();

            if(!string.IsNullOrEmpty(refNo))
            {
                stockAdjustment = await _context.StockAdjustments
                    .Where(sa => sa.RefNo == refNo)
                    .Include(sa => sa.StockAdjustmentItems)
                    .FirstOrDefaultAsync();
                    
            }
            else
            {
                stockAdjustment = await _context.StockAdjustments
                    .OrderByDescending(sa => sa.Id)
                    .Include(sa => sa.StockAdjustmentItems)
                    .FirstOrDefaultAsync() ?? new StockAdjustment();
            }

            ViewBag.Reference = await _context.StockAdjustments
                .OrderByDescending(sa => sa.Id)
                .Select(sa => sa.RefNo)
                .FirstOrDefaultAsync();

            return View(stockAdjustment);
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromBody] StockAdjustment model)
        {
            if (model == null || model.StockAdjustmentItems == null || !model.StockAdjustmentItems.Any())
                return BadRequest("No items found to save.");



            _context.StockAdjustments.Add(model);

            foreach (var adjItem in model.StockAdjustmentItems)
            {
                var dbItem = await _context.Items
                    .FirstOrDefaultAsync(i => i.ItemCode == adjItem.ItemCode);

                if (dbItem != null)
                {

                    // Update dbItem.Quantity with new physical stock
                    dbItem.Quantity = adjItem.PhysicalStock ?? dbItem.Quantity;

                    _context.Items.Update(dbItem);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = model.Id });
        }

    }
}

