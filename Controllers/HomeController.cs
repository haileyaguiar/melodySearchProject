using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Threading.Tasks;
using melodySearchProject.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text.RegularExpressions;

public class HomeController : Controller
{
    private string javaServerUrl = "http://18.216.198.21:5000/searchMusic";
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }



    [HttpPost]
    public async Task<ActionResult> SaveMeiData(string inputData)
    {
        MeiRequest mei = new MeiRequest(inputData);
        var jsonInput = JsonSerializer.Serialize(mei);

        try
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonInput, System.Text.Encoding.UTF8, "application/json");
                Debug.WriteLine($"Sending POST request to {javaServerUrl} with content: {jsonInput}");
                HttpResponseMessage response = await client.PostAsync(javaServerUrl, content);

                Debug.WriteLine($"Received response: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Response Data: {responseData}");

                    if (!string.IsNullOrEmpty(responseData))
                    {
                        try
                        {
                            Response responseObj = JsonSerializer.Deserialize<Response>(responseData);
                            if (responseObj != null && responseObj.Hits != null)
                            {
                                // Create clickable links
                                var links = responseObj.Hits.Select(hit =>
                                    $"<a href='#' class='hit-link' data-id='{hit.id}' data-name='{hit.source.name}' " +
                                    $"data-intervals='{hit.source.intervals_text}'>{hit.id}</a><br/>");

                                string linksHtml = string.Join("\n", links);
                                return Json(new { responseData = linksHtml });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error during deserialization: {ex.Message}");
                            return Json($"Error during deserialization: {ex.Message}");
                        }
                    }
                    else
                    {
                        return Json("Response data was null or empty!");
                    }
                }
                else
                {
                    Debug.WriteLine($"Error: {response.ReasonPhrase}");
                    return Json($"Error occurred while sending data to Java server: {response.ReasonPhrase}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Request error: {ex.Message}");
            return Json($"An error occurred: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"General error: {ex.Message}");
            return Json($"An error occurred: {ex.Message}");
        }

        return Json("Unexpected error occurred. No data processed.");
    }





    public IActionResult Index()
    {
        return View();
    }

    // FOR TESTING::
    public IActionResult Data()
    {
        var melodies = _context.MeiFiles.ToList();
        return View(melodies); // Ensure you have a corresponding view
    }

    public IActionResult DisplayResponse(string responseData)
    {
        string decodedData = System.Net.WebUtility.UrlDecode(responseData);
        ViewBag.ResponseData = decodedData;
        return View("DisplayResponse", ViewBag);
    }



    public class Hit
    {
        public string id { get; set; }
        public float score { get; set; }
        public Source source { get; set; }
    }

    public class Source
    {
        public string name { get; set; }
        public string intervals_text { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("hits")]
        public List<Hit> Hits { get; set; }
    }


    public class MeiRequest
    {
        public MeiRequest(string v) => meiChunk = v;
        public string meiChunk { get; set; }
    }


    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        // Allow letters (including accented), digits, and spaces
        if (!Regex.IsMatch(query, @"^[\p{L}\p{N}\s]*$", RegexOptions.Compiled))
        {
            return BadRequest("Invalid characters in search query.");
        }

        query = query?.Trim();

        // Limit the length of the search query to prevent overly long input
        if (query.Length > 100)
        {
            return BadRequest("Query is too long.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return View("SearchResults", new List<MeiFile>());
        }

        var parameter = $"%{query}%";
        var results = await _context.MeiFiles
            .FromSqlRaw("SELECT file_id, file_name, file_content FROM public.\"meiFiles\" WHERE CAST(file_content AS TEXT) ILIKE {0} ORDER BY file_name", parameter)
            .Select(m => new MeiFile { file_id = m.file_id, file_name = m.file_name })
            .ToListAsync();

        return View("SearchResults", results);
    }




    [HttpGet]
    public async Task<IActionResult> DisplayFile(int id)
    {
        var file = await _context.MeiFiles
            .FromSqlRaw("SELECT file_id, file_name, file_content FROM public.\"meiFiles\" WHERE file_id = {0}", id)
            .Select(m => new { m.file_name, m.file_content })
            .FirstOrDefaultAsync();

        if (file == null)
        {
            return NotFound(); // File doesn't exist
        }

        ViewBag.FileName = file.file_name;
        ViewBag.FileContent = file.file_content; // Pass raw MEI content to the view

        return View("DisplayFile");
    }



    [HttpGet]
    public IActionResult DownloadFile(string fileName)
    {
        // Fetch the file content from the database
        var file = _context.MeiFiles
            .FromSqlRaw("SELECT file_id, file_name, file_content FROM public.\"meiFiles\" WHERE file_name = {0}", fileName)
            .Select(m => new { m.file_name, m.file_content })
            .FirstOrDefault();

        if (file == null)
        {
            return NotFound(); // Handle the case where the file doesn't exist
        }

        // Convert the file content to a byte array
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(file.file_content);

        // Set the content disposition header to prompt download
        return File(fileBytes, "text/plain", $"{file.file_name}");
    }



    [HttpPost]
    public async Task<IActionResult> PartialSheetMusic([FromBody] ReqPartialMusic request)
    {
        // Use the request data to fetch or process the partial sheet music
        Debug.WriteLine($"Received source: {request.Source.Name}, highlight: {string.Join(",", request.Highlight)}");

        // Simulate processing and return response
        // Here you could query your database or external service to get partial sheet music data
        var sheetMusicData = "Partial sheet music based on request...";

        return Json(new { sheetMusic = sheetMusicData });
    }


    public class ReqPartialMusic
    {
        public Record Source { get; set; }
        public Dictionary<string, List<string>> Highlight { get; set; }
    }

    public class Record
    {
        public string Name { get; set; }
        public string Intervals_Text { get; set; }
        public string Measure_Map { get; set; }
        public int[] Intervals_As_Array { get; set; }
        public int[] Measure_Map_As_Array { get; set; }
    }





}



