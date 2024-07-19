using System.Collections.Generic;
using System.Linq;

namespace melodySearchProject.Models
{
    public class EFMelodyRepository : IMelodyRepository
    {
        private ApplicationDbContext _context;

        public EFMelodyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MeiFile> Meis => _context.MeiFiles.ToList();
    }
}
