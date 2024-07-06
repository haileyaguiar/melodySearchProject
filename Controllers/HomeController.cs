using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private string javaServerUrl = "http://18.226.34.115:5000/searchMusic";

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
                                return RedirectToAction("DisplayResponse", new { responseData = System.Net.WebUtility.UrlEncode(listToString) });
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

    public ActionResult Index()
    {
        return View();
    }

    public ActionResult DisplayResponse(string responseData)
    {
        string decodedData = System.Net.WebUtility.UrlDecode(responseData);
        ViewBag.ResponseData = decodedData;
        return View();
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
}


    // public class Record
    // {
    //     public string Name { get; set; }
    //     public string IntervalsText { get; set; }
    //     public List<int> IntervalsVector { get; set; }
    // }
    //
    // public class Hit<T>
    // {
    //     public string Index { get; set; }
    //     public string Id { get; set; }
    //     public float Score { get; set; }
    //     public T Source { get; set; }
    // }
    //
    // public class Response
    // {
    //     [JsonPropertyName("hits")]
    //     [JsonConverter(typeof(HitArrayConverter<Record>))]
    //     public Hit<Record>[] Hits { get; set; }
    //     public string Message { get; set; }
    //     public bool Success { get; set; }
    // }
    // public class HitConverter<T> : JsonConverter<Hit<T>>
    // {
    //     public override Hit<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //     {
    //         if (reader.TokenType != JsonTokenType.StartObject)
    //             throw new JsonException();
    //
    //         var hit = new Hit<T>();
    //
    //         while (reader.Read())
    //         {
    //             if (reader.TokenType == JsonTokenType.EndObject)
    //                 return hit;
    //
    //             if (reader.TokenType == JsonTokenType.PropertyName)
    //             {
    //                 string propertyName = reader.GetString();
    //                 reader.Read();
    //
    //                 switch (propertyName)
    //                 {
    //                     case "index":
    //                         hit.Index = reader.GetString();
    //                         break;
    //                     case "id":
    //                         hit.Id = reader.GetString();
    //                         break;
    //                     case "score":
    //                         hit.Score = reader.GetSingle();
    //                         break;
    //                     case "source":
    //                         hit.Source = JsonSerializer.Deserialize<T>(ref reader, options);
    //                         break;
    //                     default:
    //                         reader.Skip();
    //                         break;
    //                 }
    //             }
    //         }
    //
    //         throw new JsonException();
    //     }
    //
    //     public override void Write(Utf8JsonWriter writer, Hit<T> value, JsonSerializerOptions options)
    //     {
    //         throw new NotImplementedException("Serialization not implemented for Hit<T>");
    //     }
    // }
    //
    // public class HitArrayConverter<T> : JsonConverter<Hit<T>[]>
    // {
    //     public override Hit<T>[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //     {
    //         var hits = new List<Hit<T>>();
    //
    //         if (reader.TokenType != JsonTokenType.StartArray)
    //             throw new JsonException();
    //
    //         while (reader.Read())
    //         {
    //             if (reader.TokenType == JsonTokenType.EndArray)
    //                 return hits.ToArray();
    //
    //             var hit = JsonSerializer.Deserialize<Hit<T>>(ref reader, options);
    //             hits.Add(hit);
    //         }
    //
    //         throw new JsonException();
    //     }
    //
    //     public override void Write(Utf8JsonWriter writer, Hit<T>[] value, JsonSerializerOptions options)
    //     {
    //         throw new NotImplementedException("Serialization not implemented for Hit<T>[]");
    //     }
    // };

