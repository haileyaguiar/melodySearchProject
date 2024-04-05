
namespace melodySearchProject.Models
{
    public class EFMelodyRepository : IMelodyRepository
    {
        private TestMelodiesContext _context;

        public EFMelodyRepository(TestMelodiesContext temp) 
        {
            _context = temp;
        }
        public List<Mei> Meis => _context.Meis.ToList();
    }
}
