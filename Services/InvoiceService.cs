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
                var today = DateTime.Today;

                // Try to get today's invoice sequence
                var sequence = _context.InvoiceSequences.FirstOrDefault(x => x.Date == today);

                if (sequence == null)
                {
                    // New day → start from 0001
                    sequence = new InvoiceSequence
                    {
                        Date = today,
                        LastNumber = 0,
                        IsClosed = false
                    };
                    _context.InvoiceSequences.Add(sequence);
                    _context.SaveChanges();
                }

                // Continue from last number
                sequence.LastNumber++;
                _context.SaveChanges();

                transaction.Commit();

                // Format invoice: INV-MM-dd-yyyy-0001
                string datePart = today.ToString("MM-dd-yyyy");
                string numberPart = sequence.LastNumber.ToString("D4");

                return $"INV-{datePart}-{numberPart}";
            }
        }

        public void EndDay()
        {
            var today = DateTime.Today;
            var sequence = _context.InvoiceSequences.FirstOrDefault(x => x.Date == today);
            if (sequence != null)
            {
                sequence.IsClosed = true;
                _context.SaveChanges();
            }
        }
    }
}
