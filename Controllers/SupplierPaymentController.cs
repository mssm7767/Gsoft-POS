using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class SupplierPaymentController : Controller
    {
        private readonly AppDbContext _context;
        public SupplierPaymentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /SupplierPayment/Receive
        [HttpGet]
        public IActionResult Receive()
        {
            var suppliers = _context.Suppliers.OrderBy(s => s.SupplierName).ToList();
            var payments = _context.SupplierPayments.OrderByDescending(p => p.PaymentDate).Take(50).ToList();

            var model = new SupplierReceivePaymentViewModel
            {
                SupplierList = suppliers,
                PaymentHistory = payments,
                PaymentDate = DateTime.Now,
                ReceivedBy = User.Identity?.Name ?? "Admin"
            };
            return View(model);
        }

        // POST: /SupplierPayment/Receive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Receive(SupplierReceivePaymentViewModel model)
        {
            ModelState.Clear();
            if (!ModelState.IsValid)
            {
                model.SupplierList = _context.Suppliers.OrderBy(s => s.SupplierName).ToList();
                model.PaymentHistory = _context.SupplierPayments.OrderByDescending(p => p.PaymentDate).Take(50).ToList();
                return View(model);
            }

            var supplier = _context.Suppliers.FirstOrDefault(s => s.Id == model.SupplierId);
            decimal oldBalance = supplier?.OpeningBalance ?? 0;
            decimal totalPaid = _context.SupplierPayments.Where(p => p.SupplierId == model.SupplierId).Sum(p => (decimal?)p.Amount) ?? 0;
            decimal balance = oldBalance - totalPaid;
            model.ReceivedBy = "system";

            var previousPayment = _context.SupplierPayments.OrderByDescending(p => p.Id).Select(p => p.Remaining).FirstOrDefault();

            var payment = new SupplierPayment
            {
                SupplierId = model.SupplierId,
                Amount = model.Amount,
                Advance = model.Advance,
                PaymentMethod = model.PaymentMethod,
                Narration = model.Narration,
                PaymentDate = model.PaymentDate,
                Remaining = previousPayment - model.Amount ?? 0 - model.Advance ?? 0,
                ReceivedBy = model.ReceivedBy
            };

            _context.SupplierPayments.Add(payment);
            _context.SaveChanges();

            supplier.OpeningBalance = payment.Remaining;
            _context.Suppliers.Update(supplier);
            _context.SaveChanges();



            TempData["PaymentSuccess"] = "Supplier payment saved!";
            return RedirectToAction(nameof(Receive));
        }

        [HttpGet]
        public IActionResult GetSupplierBalance(int supplierId)
        {
            // Latest balance (remaining) for this customer
            var balance = _context.Suppliers
                                  .Where(c => c.Id == supplierId)
                                  .Select(c => (decimal?)c.OpeningBalance) // ensure scalar
                                  .FirstOrDefault() ?? 0m;

            return Json(new { balance });
        }
    }
}

