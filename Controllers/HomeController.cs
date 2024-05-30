using melodySearchProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

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
            MeiRequest mei = new MeiRequest(inputData);
            var jsonINpout = JsonSerializer.Serialize(mei);

            try{
                using (var client = new HttpClient()){
                    var content = new StringContent(jsonINpout, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(javaServerUrl, content);
                    if (response.IsSuccessStatusCode){

                        //First get the response data as a string then deserialize it
                        string responseData = await response.Content.ReadAsStringAsync();
                        Response responseObj;
                        List<string> names = new List<string>();

                        //if it isn't null, deserailize and add all the ids to a list to display
                        if(responseData != null){
                            responseObj = JsonSerializer.Deserialize<Response>(responseData);
                            //If the deserialized isn't null, then add all the names to a list
                            if(responseObj != null){
                                foreach(Hit<Record> h in responseObj.hits){names.Add(h.Id);}
                            } else {
                                string objNull = "Obj was null!\n";
                                Console.WriteLine(objNull);
                                Debug.WriteLine(objNull);
                                return Json(objNull);
                            }
                        } else {
                            string responseNull = "Resonse data was null!\n";
                            Console.WriteLine(responseNull);
                            Debug.WriteLine(responseNull); 
                            return Json(responseNull);
                        }
                        // Redirect to a new action with the response data as a query parameter
                        string listToString = "";
                        foreach(string s in names){
                            listToString = listToString + s + "\n";
                        }
                        return RedirectToAction("DisplayResponse", listToString);
                    } else {
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
            return View(responseData);
        }

    }

        public class MeiRequest{
            public MeiRequest(string v){
                meiChunk = v;
            }
            public string meiChunk { get; set; }
        }

        public class Record{
            public string name { get; set; }
            public string intervals_text { get; set; }
            public int[] intervals_vector { get; set; }
        }

        public class Response{
            public Hit<Record>[] hits { get; set; }
        }
};


