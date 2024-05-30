using melodySearchProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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
            MeiRequest mei = new MeiRequest("""
                   <measure n="7" xml:id="w796839ab1c27">
                     <staff n="1">
                        <layer n="1">
                           <note accid.ges="s"
                                 dur="4"
                                 oct="4"
                                 pname="f"
                                 stem.dir="up"
                                 xml:id="w796839ab1c27b1b1"/>
                           <note dur="4"
                                 oct="4"
                                 pname="g"
                                 stem.dir="up"
                                 xml:id="w796839ab1c27b1b3"/>
                           <note dur="4"
                                 oct="4"
                                 pname="a"
                                 stem.dir="up"
                                 xml:id="w796839ab1c27b1b5"/>
                           <note accid.ges="s"
                                 dur="4"
                                 oct="4"
                                 pname="f"
                                 stem.dir="up"
                                 xml:id="w796839ab1c27b1b7"/>
                        </layer>
                     </staff>
                  </measure>
                """);
            var jsonINpout = JsonSerializer.Serialize(mei);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonINpout, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(javaServerUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = await response.Content.ReadAsStringAsync();

                        // Redirect to a new action with the response data as a query parameter
                        return RedirectToAction("DisplayResponse", new { responseData = responseData });
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



        public ActionResult DisplayResponse(string responseData)
        {
            // Pass the response data to the view
            ViewBag.ResponseData = responseData;
            return View();
        }

    }

        public class MeiRequest{
            public MeiRequest(string v){
                meiChunk = v;
            }
            public string meiChunk { get; set; }
        }
};


