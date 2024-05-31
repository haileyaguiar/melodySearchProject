using melodySearchProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

public class MeiRequest
{
    public MeiRequest(string v) => meiChunk = v;
    public string meiChunk { get; set; }
}

// public class Record
// {
//     public string Name { get; set; }
//     public string IntervalsText { get; set; }
//     public List<int> IntervalsVector { get; set; }
// }

// public class Hit<T>
// {
//     public string Index { get; set; }
//     public string Id { get; set; }
//     public float Score { get; set; }
//     public T Source { get; set; }
// }

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

//         var hit = new Hit<T>();

//         while (reader.Read())
//         {
//             if (reader.TokenType == JsonTokenType.EndObject)
//                 return hit;

//             if (reader.TokenType == JsonTokenType.PropertyName)
//             {
//                 string propertyName = reader.GetString();
//                 reader.Read();

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

//         throw new JsonException();
//     }

//     public override void Write(Utf8JsonWriter writer, Hit<T> value, JsonSerializerOptions options)
//     {
//         throw new NotImplementedException("Serialization not implemented for Hit<T>");
//     }
// }

// public class HitArrayConverter<T> : JsonConverter<Hit<T>[]>
// {
//     public override Hit<T>[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var hits = new List<Hit<T>>();

//         if (reader.TokenType != JsonTokenType.StartArray)
//             throw new JsonException();

//         while (reader.Read())
//         {
//             if (reader.TokenType == JsonTokenType.EndArray)
//                 return hits.ToArray();

//             var hit = JsonSerializer.Deserialize<Hit<T>>(ref reader, options);
//             hits.Add(hit);
//         }

//         throw new JsonException();
//     }

//     public override void Write(Utf8JsonWriter writer, Hit<T>[] value, JsonSerializerOptions options)
//     {
//         throw new NotImplementedException("Serialization not implemented for Hit<T>[]");
//     }
// };

public class Response {
    public List<string> Names {get; set;}
    public string Message { get; set; }
    public bool Success { get; set; }

}

public class HomeController : Controller
{
    private string javaServerUrl = "http://localhost:5000/searchMusic";

    [HttpPost]
    public async Task<ActionResult> SaveMeiData(string inputData)
    {
        MeiRequest mei = new MeiRequest(inputData);
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
                    if (!string.IsNullOrEmpty(responseData))
                    {
                        Response responseObj = JsonSerializer.Deserialize<Response>(responseData);
                        if (responseObj != null && responseObj.Names != null)
                        {
                            List<string> names = responseObj.Names;
                            // Redirect to a new action with the response data as a query parameter
                            string listToString = string.Join("\n", names);
                            Console.WriteLine(listToString);
                            Debug.WriteLine(listToString);
                            return Json(listToString);
                            //return RedirectToAction("DisplayResponse", new { responseData = listToString });
                        }
                        else
                        {
                            string objNull = "Deserialized object or Hits property was null!\n";
                            Console.WriteLine(objNull);
                            Debug.WriteLine(objNull);
                            return Json(objNull);
                        }
                    }
                    else
                    {
                        string responseNull = "Response data was null or empty!\n";
                        Console.WriteLine(responseNull);
                        Debug.WriteLine(responseNull);
                        return Json(responseNull);
                    }
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
        return View(responseData);
    }
};