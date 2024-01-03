
using M3_Ybs;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using Windows.UI.Xaml;
using Newtonsoft.Json.Linq;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class JsonController : ControllerBase
{
    private readonly ILogger<JsonController> _logger;
    private readonly HttpClient _httpClient;




    public JsonController(ILogger<JsonController> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }



    private const string ApiEndpoint = "https://10.3.156.130:7069/api/Home/GetRoute";
    private const string JsonDosyaAdi = "stationData.json";



    [HttpGet("GetData")]
    public async Task<IActionResult> GetData()
    {


        int retryCount = 0;
        const int maxRetryCount = 5; // Maximum number of retries





        try
        {
            //var apiEndpoint = "https://192.168.1.5:7069/api/Home/GetRoute";

            //SSL hatalarını kontrol etmeksizin bir HTTP isteği yapmak için kullanılır.
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


            HttpResponseMessage response;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            var client = new HttpClient(handler);

            //var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            response = await client.GetAsync(ApiEndpoint);


            //var resData=JsonConvert.DeserializeObject<Stations>(response.ToString());
            //Debug.WriteLine(resData)

            if (response.IsSuccessStatusCode)
            {
                //var responseData = response.Content.ReadAsStringAsync();

                string responseData = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseData);

                string? firstKey = jsonObject.Properties().Select(p => p.Name).FirstOrDefault();



                GlobalVariablesDTO.metroName = firstKey;

                JToken istasyonlarToken = jsonObject[firstKey];


                var newStationData = JsonConvert.DeserializeObject<List<Stations>>(istasyonlarToken.ToString());//Okudugumuz json dosyasını Stations class ına göre dönüstürüp kullanacagız


                //var station = JsonConvert.DeserializeObject<List<Stations>>(newStationData); //Okudugumuz json dosyasını Stations class ına göre dönüstürüp kullanacagız

                string jsonData = JsonConvert.SerializeObject(newStationData);




                if (!System.IO.File.Exists(JsonDosyaAdi))
                {
                    await System.IO.File.WriteAllTextAsync(JsonDosyaAdi, jsonData);
                    Debug.WriteLine("İlk defa JSON dosyası oluşturuldu");
                }
                else
                {
                    string existingData = await System.IO.File.ReadAllTextAsync(JsonDosyaAdi);

                    if (existingData != jsonData)
                    {
                        await System.IO.File.WriteAllTextAsync(JsonDosyaAdi, jsonData);
                        Debug.WriteLine("JSON dosyası güncellendi");
                        Stations.IsRouteChanged = true;
                    }
                    else
                    {
                        Debug.WriteLine("JSON dosyası değişmedi");
                        Stations.IsRouteChanged = false;
                    }
                }

                return Ok(newStationData);

            }
            else
            {
                _logger.LogError("API isteği başarısız oldu: " + response.StatusCode);
                retryCount++;

                // Check if retry limit has been reached
                if (retryCount >= maxRetryCount)
                {
                    // Retry limit reached, return error
                    return StatusCode(500, "API isteği sırasında bir hata oluştu");
                }

                // Retry the request after a delay
                await Task.Delay(1000); // Delay for 1 second
                return StatusCode((int)response.StatusCode, "API isteği başarısız oldu");
            }
        }
        catch (HttpRequestException ex)
        {
            Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "HTTP Get Rota okuma sırasında hata olustu");

            _logger.LogError("API isteği başarısız oldu: ");
            retryCount++;

            // Check if retry limit has been reached
            if (retryCount >= maxRetryCount)
            {
                // Retry limit reached, return error
                return StatusCode(500, "API isteği sırasında bir hata oluştu");
            }

            // Retry the request after a delay
            await Task.Delay(1000); // Delay for 1 second
          

            return StatusCode(500, "API isteği sırasında bir HTTP hatası oluştu. Ayrıntılar için log dosyasını kontrol edin.");
        }
        catch (TaskCanceledException ex)
        {
            Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "HTTP Get Rota okuma sırasında hata olustu");
            return StatusCode(500, "API isteği zaman aşımına uğradı. Lütfen tekrar deneyin.");
        }
        catch (Exception ex)
        {
            Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "HTTP Get Rota okuma sırasında hata olustu");
            return StatusCode(500, "API isteği sırasında bir hata oluştu. Ayrıntılar için log dosyasını kontrol edin.");
        }

    }


}


