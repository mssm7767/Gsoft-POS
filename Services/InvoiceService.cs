using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _context;

        public InvoiceService(AppDbContext context)
        {
            _context = context;
        }

        public string GenerateInvoiceNumber()
        {
            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                // 🔹 Get active (not closed) business day
                var activeDay = _context.InvoiceSequences
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefault(x => x.IsClosed == false);

                if (activeDay == null)
                {
                    throw new Exception("No active business day found. Please open a day first.");
                }

                var workingDate = activeDay.Date;

                // 🔹 Get sequence for that business date
                var sequence = _context.InvoiceSequences
                    .FirstOrDefault(x => x.Date == workingDate);

                if (sequence == null)
                {
                    sequence = new InvoiceSequence
                    {
                        Date = workingDate,
                        LastNumber = 0,
                        IsClosed = false
                    };

                    _context.InvoiceSequences.Add(sequence);
                    _context.SaveChanges();
                }

                // 🔹 Increment safely
                sequence.LastNumber++;
                _context.SaveChanges();

                transaction.Commit();

                // 🔹 Format invoice
                string datePart = workingDate.ToString("MM-dd-yyyy");
                string numberPart = sequence.LastNumber.ToString("D4");

                return $"INV-{datePart}-{numberPart}";
            }
        }
        public void EndDay()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                // 🔹 Get current active (open) business day
                var activeDay = _context.InvoiceSequences
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefault(x => x.IsClosed == false);

                if (activeDay == null)
                {
                    throw new Exception("No active business day found.");
                }

                // 🔒 Close current day
                activeDay.IsClosed = true;
                _context.SaveChanges();

                // ➕ Create new business day (next logical day)
                var newDay = new InvoiceSequence
                {
                    Date = DateTime.Now,
                    LastNumber = 0,
                    IsClosed = false
                };

                _context.InvoiceSequences.Add(newDay);
                _context.SaveChanges();

                transaction.Commit();
            }
        }
    }
}
