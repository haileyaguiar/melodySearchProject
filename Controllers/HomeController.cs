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
using Elastic.Clients.Elasticsearch.Core.Search;

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
                                // Stringify the indices from each hit object
                                string listToString = string.Join("\n", responseObj.Hits.Select(hit => hit.Id));

                                Debug.WriteLine("It got here!");

                                return Json(new { responseData = listToString });
                            }
                            else
                            {
                                return Json("Hits property was null or deserialized object was null!");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Deserialization error: {ex.Message}");
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

    public class Response
    {
        [JsonPropertyName("hits")]
        public List<Hit> Hits { get; set; }
    }


    public class Hit
    {
        [JsonPropertyName("index")]
        public string Index { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, object>? Fields { get; set; }

        [JsonPropertyName("highlight")]
        public Highlight? Highlight { get; set; }

        [JsonPropertyName("innerHits")]
        public Dictionary<string, object>? InnerHits { get; set; }

        [JsonPropertyName("matchedQueries")]
        public List<string>? MatchedQueries { get; set; }

        [JsonPropertyName("nested")]
        public object? Nested { get; set; }

        [JsonPropertyName("ignored")]
        public List<string>? Ignored { get; set; }

        [JsonPropertyName("ignoredFieldValues")]
        public Dictionary<string, object>? IgnoredFieldValues { get; set; }

        [JsonPropertyName("shard")]
        public object? Shard { get; set; }

        [JsonPropertyName("node")]
        public object? Node { get; set; }

        [JsonPropertyName("routing")]
        public object? Routing { get; set; }

        [JsonPropertyName("source")]
        public Record? Record { get; set; }

        [JsonPropertyName("seqNo")]
        public int? SeqNo { get; set; }

        [JsonPropertyName("primaryTerm")]
        public int? PrimaryTerm { get; set; }

        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("sort")]
        public List<string>? Sort { get; set; }
    }

    public class Highlight
    {
        [JsonPropertyName("intervals_text")]
        public List<string>? IntervalsText { get; set; }
    }

    public class Record
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("intervals_text")]
        public string IntervalsText { get; set; }

        [JsonPropertyName("measure_map")]
        public string MeasureMap { get; set; }

        [JsonPropertyName("intervals_as_array")]
        public List<int>? IntervalsAsArray { get; set; }

        [JsonPropertyName("measure_map_as_array")]
        public List<int>? MeasureMapAsArray { get; set; }
    }



    public class MeiRequest
    {
        public MeiRequest(string v) => meiChunk = v;
        public string meiChunk { get; set; }
    }


    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View("SearchResults", new List<MeiFile>());
        }

        var results = await _context.MeiFiles
            .FromSqlRaw("SELECT file_id, file_name, file_content FROM public.\"meiFiles\" WHERE CAST(file_content AS TEXT) ILIKE {0} ORDER BY file_name", $"%{query}%")
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


}



