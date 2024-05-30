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


        [HttpGet]
        public IActionResult MelodySearch()
        {
            return View();
        }



        [HttpPost]
        public async Task<ActionResult> SaveMeiData(string inputData)
        {
            string javaServerUrl = "http://localhost:5000/searchMusic"; // Replace with your Java server URL

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(inputData, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(javaServerUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                        return Json(responseData);
                    }
                    else
                    {
                        return Json("Error occurred while sending data to Java server.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return Json($"An error occurred: {ex.Message}");
            }
        }
}
};


