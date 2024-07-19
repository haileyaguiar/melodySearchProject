using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Threading.Tasks;
using melodySearchProject.Models;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private string javaServerUrl = "http://13.59.226.235:5000/searchMusic";
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
                            if (responseObj != null && responseObj.Names != null)
                            {
                                string listToString = string.Join("\n", responseObj.Names);
                                Debug.WriteLine("It got here!");

                                return Json(new { responseData = listToString });
                            }
                            else
                            {
                                return Json("Names property was null or deserialized object was null!");
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
        [JsonPropertyName("names")]
        public List<string>? Names { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class MeiRequest
    {
        public MeiRequest(string v) => meiChunk = v;
        public string meiChunk { get; set; }
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View(Enumerable.Empty<MeiFile>());
        }

        // Define the SQL query for searching within the XML content
        var sql = @"
            SELECT * 
            FROM meiFiles
            WHERE xpath('//*[contains(text(), {0})]', file_content::xml) IS NOT NULL";

        // Execute the SQL query
        var results = await _context.MeiFiles
            .FromSqlRaw(sql, query)
            .ToListAsync();

        return View(results);
    }
}
