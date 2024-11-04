using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using melodySearchProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;

public class HomeController : Controller
{
    // Java server URL and endpoint for initial POST
    private string javaServerUrl = "http://18.216.198.21:5000/searchMusic";
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }


    // Post called when MEI is searched to make dynamic links
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
                //Debug.WriteLine($"Sending POST request to {javaServerUrl} with content: {jsonInput}");
                HttpResponseMessage response = await client.PostAsync(javaServerUrl, content);

                //Debug.WriteLine($"Received response: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    //Debug.WriteLine($"Response Data: {responseData}");

                    if (!string.IsNullOrEmpty(responseData))
                    {
                        try
                        {
                            Response responseObj = JsonSerializer.Deserialize<Response>(responseData);
                            foreach(Hit hit in responseObj.hits){
                                string hit_highlight = "{";
                                foreach(var pair in hit.highlight){
                                    hit_highlight += "\"";
                                    hit_highlight += pair.Key;
                                    hit_highlight += "\": [";
                                    hit_highlight += (string.Join(", ", pair.Value));
                                    hit_highlight += "], ";
                                }
                                hit_highlight += "}";
                                //Console.WriteLine(hit_highlight);
                            }
                            if (responseObj != null && responseObj.hits != null)
                            {
                                // Create clickable links
                                var links = responseObj.hits.Select(hit =>
                                    $"<a href='#' class='hit-link' data-id='{hit.id}' data-name='{hit.source.name}' " +
                                    $"data-intervals-text='{string.Join(" ", hit.source.intervals_text)}' " +
                                    $"data-intervals-as-array='{string.Join(",", hit.source.intervals_as_array)}' " +
                                    $"data-measure-map='{string.Join(" ", hit.source.measure_map)}' " +
                                    $"data-measure-map-as-array='{string.Join(",", hit.source.measure_map_as_array)}' " +
                                    $"data-highlight='{string.Join(" ", hit.highlight["intervals_text"])}'>{hit.id}</a><br/>");

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

                //Debug.WriteLine($"Sending POST request to {targetUrl} with content: {jsonInput}");
                HttpResponseMessage response = await client.PostAsync(targetUrl, content);

                //Debug.WriteLine($"Received response: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    //Debug.WriteLine($"Response Data: {responseData}");

                    // Return the response from the new API back to the frontend
                    return Json(new { success = true, response = responseData });
                }
                else
                {
                    //Debug.WriteLine($"Error: {response.ReasonPhrase}");
                    return Json(new { success = false, error = response.ReasonPhrase });
                }
            }
        }
        catch (HttpRequestException ex)
        {
            //Debug.WriteLine($"Request error: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"General error: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    // Server deserialization response objects
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
        public Dictionary<string, string[]> highlight { get; set; }

        [JsonPropertyName("source")]
        public Source source { get; set; }
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

    public class ReqSearchMusic
    {
        public ReqSearchMusic(string v) => meiChunk = v;

        [JsonPropertyName("meiChunk")]
        public string meiChunk { get; set; }
    }

    public class ReqPartialMusic
    {
        [JsonPropertyName("source")]
        public Source source { get; set; }

        [JsonPropertyName("highlight")]
        public Dictionary<string, string[]> highlight { get; set; }
    }



    public IActionResult Index()
    {
        HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        HttpContext.Response.Headers["Pragma"] = "no-cache";
        HttpContext.Response.Headers["Expires"] = "0";
        return View();
    }


    // Text search method
    [HttpGet]
    public async Task<IActionResult> SearchAdvanced(string keyword, string title, string composer, string librettist, string incipit, string musForm, string poetForm, string cdcNumber, string logicalOperator)
    {
        // Prepare base query and search clauses
        var baseQuery = "SELECT file_id, file_name, file_content FROM public.\"meiFiles\" WHERE 1=1 ";
        var clauses = new List<string>();
        var parameters = new List<object>();
        var parameterIndex = 0; // To track parameter numbers for @p0, @p1, etc.

        // Add search clauses based on input
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            clauses.Add($"CAST(file_content AS TEXT) ILIKE @p{parameterIndex}");
            parameters.Add($"%{keyword}%");
            parameterIndex++; // Increment the parameter index
        }
        if (!string.IsNullOrWhiteSpace(title))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:title/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS title_text
                        WHERE title_text::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{title}%");
            parameterIndex++;
        }
        if (!string.IsNullOrWhiteSpace(composer))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:composer/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS composer_text
                        WHERE composer_text::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{composer}%");
            parameterIndex++;
        }
        if (!string.IsNullOrWhiteSpace(librettist))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:librettist/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS librettist_text
                        WHERE librettist_text::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{librettist}%");
            parameterIndex++;
        }
        if (!string.IsNullOrWhiteSpace(musForm))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:notes[@type=''musical form'']/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS musForm_text
                        WHERE musForm_text::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{musForm}%");
            parameterIndex++;
        }
        if (!string.IsNullOrWhiteSpace(poetForm))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:notes[@type=''poetic form'']/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS poetForm_text
                        WHERE poetForm_text::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{poetForm}%");
            parameterIndex++;
        }
        if (!string.IsNullOrWhiteSpace(cdcNumber))
        {
            clauses.Add($@"EXISTS (
                        SELECT 1
                        FROM unnest(xpath(
                            '//ns:notes[@type=''CdC Number'']/text()',
                            file_content::xml,
                            ARRAY[ARRAY['ns', 'http://www.music-encoding.org/ns/mei']]
                        )) AS cdc_number
                        WHERE cdc_number::text ILIKE @p{parameterIndex}
                    )");
            parameters.Add($"%{cdcNumber}%");
            parameterIndex++;
        }

        // Combine clauses using the selected logical operator
        if (clauses.Any())
        {
            // Handle 'NOT' by wrapping the combined conditions
            if (logicalOperator == "NOT")
            {
                var combinedClauses = string.Join(" AND ", clauses);  // Use AND between clauses for 'NOT'
                baseQuery += " AND NOT (" + combinedClauses + ")";
            }
            else
            {
                var combinedClauses = string.Join($" {logicalOperator} ", clauses);
                baseQuery += " AND (" + combinedClauses + ")";
            }
        }

        var results = await _context.MeiFiles
            .FromSqlRaw(baseQuery, parameters.ToArray())
            .Select(m => new MeiFile { file_id = m.file_id, file_name = m.file_name })
            .ToListAsync();

        return View("SearchResults", results);
    }












    // Method called when the Name search bar is used
    // Currently the name search bar is not in use
    [HttpGet]
    public async Task<IActionResult> SearchName(string query)
    {
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



    // Displays the MEI file when a text search link is clicked
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


    // Downloads the file to the user's computer
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
