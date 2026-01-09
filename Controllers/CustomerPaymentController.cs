using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace GSoftPosNew.Controllers
{

    public class CustomerPaymentController : Controller
    {
        private readonly AppDbContext _context;
        public CustomerPaymentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /CustomerPayment/Receive
        [HttpGet]
        public IActionResult Receive()
        {
            var customers = _context.Customers.OrderBy(c => c.CustomerName).ToList();
            var payments = _context.CustomerPayments.OrderByDescending(p => p.PaymentDate).Take(50).ToList();

            var model = new ReceivePaymentViewModel
            {
                CustomerList = customers,
                PaymentHistory = payments,
                PaymentDate = DateTime.Now,
                ReceivedBy = User.Identity?.Name ?? "Admin"
            };
            return View(model);
        }
        [HttpGet]
        public IActionResult GetCustomerBalance(int customerId)
        {
            // Latest balance (remaining) for this customer
            var balance = _context.CustomerPayments
                                  .Where(c => c.CustomerId == customerId)
                                  .OrderByDescending(c => c.PaymentDate)
                                  .Select(c => (decimal?)c.Remaining) // ensure scalar
                                  .FirstOrDefault() ?? 0m;

            return Json(new { balance });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Receive(ReceivePaymentViewModel model)
        {
            // Allow null values → clear model state errors
            ModelState.Clear();

            var customer = _context.Customers.FirstOrDefault(c => c.Id == model.CustomerId);
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName = "Unknown Customer",
                    OpeningBalance = 0
                };
                _context.Customers.Add(customer);
                _context.SaveChanges();
            }

            // Get last remaining balance
            var previousRemaining = _context.CustomerPayments
                    .Where(p => p.CustomerId == model.CustomerId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => (decimal?)p.Remaining)
                    .FirstOrDefault();


            var paymentAmount = model.Amount > 0 ? model.Amount : 0;
            //var newRemaining = previousRemaining - paymentAmount;
            decimal advance = model.Advance;

            
            


            // 👉 Check if record already exists for this CustomerId
            var existingPayment = _context.CustomerPayments
                .FirstOrDefault(p => p.CustomerId == model.CustomerId);

            decimal newRemaining = 0;

            if(existingPayment != null)
            {
                newRemaining = existingPayment.Remaining;
            }

            if (advance > 0)
            {
                newRemaining = (previousRemaining ?? 0) + advance;
            }

            if(paymentAmount > 0)
            {
                newRemaining = (previousRemaining ?? 0) + paymentAmount;
            }

            if (existingPayment != null)
            {
                // Update record
                existingPayment.Amount += paymentAmount;  // accumulate amount
                existingPayment.Advance += advance;      // accumulate advance
                existingPayment.PaymentMethod = model.PaymentMethod ?? "Cash";
                existingPayment.Narration = model.Narration ?? "Payment";
                existingPayment.PaymentDate = DateTime.Now;
                existingPayment.Remaining = newRemaining;
                existingPayment.ReceivedBy = model.ReceivedBy ?? "System";

                _context.CustomerPayments.Update(existingPayment);
            }
            else
            {
                // Insert new record
                var payment = new CustomerPayment
                {
                    CustomerId = model.CustomerId,
                    Amount = paymentAmount,
                    Advance = advance,
                    PaymentMethod = model.PaymentMethod ?? "Cash",
                    Narration = model.Narration ?? "Payment",
                    PaymentDate = DateTime.Now,
                    Remaining = customer.OpeningBalance ?? 0 + newRemaining,
                    ReceivedBy = model.ReceivedBy ?? "System"
                };

                _context.CustomerPayments.Add(payment);

                customer.OpeningBalance = payment.Remaining;

                _context.Customers.Update(customer);
            }

            _context.SaveChanges();

            TempData["PaymentSuccess"] = "Payment saved successfully!";
            return RedirectToAction(nameof(Receive));
        }


    }
}

