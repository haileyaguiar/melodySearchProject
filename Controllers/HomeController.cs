using melodySearchProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace melodySearchProject.Controllers
{
    public class HomeController : Controller
    {
        public IMelodyRepository _repo;
  
        public HomeController(IMelodyRepository temp) 
        {
            _repo = temp;
        }

        public IActionResult Index()
        {
            ViewBag.MEI = _repo.Meis.ToList();
            return View();
        }

        public IActionResult Search(string query)
        {
            // Perform search based on query
            var searchResults = _repo.Meis
                                          .Where(f => f.FileData.Contains(query))
                                          .ToList();

            return View(searchResults);
        }

        [HttpPost]
        public IActionResult MelodySearch(string query)
        {
            query

            search_results = 

            return View(search_results);
        }


    }
}
