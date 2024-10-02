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
        ReqSearchMusic mei = new ReqSearchMusic(inputData);
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
                            if (responseObj != null && responseObj.hits != null)
                            {
                                // Create clickable links
                                var links = responseObj.hits.Select(hit =>
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


    //Start server deserialization response objects
    public class Response
    {
        [JsonPropertyName("hits")]
        public Hit[] hits { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("success")]
        public bool success { get; set; }
    }

    public class Hit
    {
        [JsonPropertyName("index")]
        public string index { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("score")]
        public float score { get; set; }

        [JsonPropertyName("highlight")]
        public Highlight highlight { get; set; }

        [JsonPropertyName("source")]
        public Source source { get; set; }
    }

    public class Highlight
    {
        [JsonPropertyName("intervals_text")]
        public string[] intervals_text { get; set; }
    }

    public class Source
    {
        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("intervals_text")]
        public string intervals_text { get; set; }

        [JsonPropertyName("measure_map")]
        public string measure_map { get; set; }

        [JsonPropertyName("intervals_as_array")]
        public int[] intervals_as_array { get; set; }

        [JsonPropertyName("measure_map_as_array")]
        public int[] measure_map_as_array { get; set; }
    }
    //End server deserialization response objects


    //Start server serialization request objects
    public class ReqSearchMusic
    {
        //This is a constructor I think
        public ReqSearchMusic(string v) => meiChunk = v;

        [JsonPropertyName("meiChunk")]
        public string meiChunk { get; set; }
    }

    public class ReqPartialMusic
    {
        [JsonPropertyName("source")]
        public Source source { get; set; }

        [JsonPropertyName("highlight")]
        public Highlight highlight { get; set; }
    }
    //End server serialization request objects


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













    // Here's the method that handles the links being clicked. It sends the data from inside the a tags to the server
    // with the endpoint /partialSheetMusic

    [HttpPost]
    public async Task<ActionResult> HandleLinkClick([FromBody] ReqPartialMusic requestData)
    {
        var jsonInput = JsonSerializer.Serialize(requestData);

        try
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonInput, System.Text.Encoding.UTF8, "application/json");

                string targetUrl = "http://18.216.198.21:5000/partialSheetMusic";

                Debug.WriteLine($"Sending POST request to {targetUrl} with content: {jsonInput}");
                HttpResponseMessage response = await client.PostAsync(targetUrl, content);

                Debug.WriteLine($"Received response: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Response Data: {responseData}");

                    // Return the response from the new API back to the frontend
                    return Json(new { success = true, response = responseData });
                }
                else
                {
                    Debug.WriteLine($"Error: {response.ReasonPhrase}");
                    return Json(new { success = false, error = response.ReasonPhrase });
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Request error: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"General error: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
    }


    // Here's the class that the method above uses. I know it's probably not correct, but IDK what it's supposed to be
    // Note: You may have to change the data that is saved in those a tags in the SaveMeiData method at the top because
    // all the data you need may not be there. If you do have to do that, you'll have to change the Javascript in the 
    // Index.cshtml file and this class. 
    public class HitData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Intervals { get; set; }
    }


}
