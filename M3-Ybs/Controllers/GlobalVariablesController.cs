// GlobalController.cs
using M3_Ybs;
using M3_Ybs.BackGroundServices;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using static M3_Ybs.BackGroundServices.Manager;

[ApiController]
//[Route("api/[controller]")]
public class GlobalController : ControllerBase
{


    [Route("api/Global/{istasyonName}")]
    //[HttpPost("stationName/{istasyonName}")]
    [HttpGet]
    public async Task<IActionResult> Get(string istasyonName)
    {
        try
        {


            // CurrentStationHolder.CurrentStation'a eriþim
            string currentStation = Stations.CurrentStation;


            if (currentStation == null)
            {
                return NotFound("CurrentStation is not set.");
            }


            Debug.WriteLine("<---------Dedeye giden istasyon-------->:" + currentStation);
            return Ok(currentStation);
        }
        catch (Exception ex)
        {
            // Hata durumunda istemciye hata mesajýný döndür
            Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "Web e giden HTTP Get sýrasýnda hata olustu");
            return BadRequest(new { Message = $"An error occurred: {ex.Message}" });
        }
    }

    [Route("api/Global/takoValue")]
    //[HttpPost("stationName/{istasyonName}")]
    [HttpGet]
    public async Task<IActionResult> GetTako()
    {
        try
        {

            var sendReaminingDistance = M3_Ybs.GlobalVariablesDTO.sendReaminingDistance;
            var sumDistance = M3_Ybs.GlobalVariablesDTO.sendDistance;

            var result =new  { sendReaminingDistance, sumDistance };

            if (sendReaminingDistance == null)
            {
                return NotFound("Tako is not set.");
            }

            Debug.WriteLine("Adalete giden tako verisi" + result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Hata durumunda istemciye hata mesajýný döndür
            Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "Web e giden HTTP Get sýrasýnda hata olustu");
            return BadRequest(new { Message = $"An error occurred: {ex.Message}" });
        }
    }
}

//public class GlobalVariablesDTO
//{
//    public bool LeftDoor_Status { get; set; }
//    public bool RightDoor_Status { get; set; }
//    public bool All_LeftDoor_Release { get; set; }
//    public bool All_RightDoor_Release { get; set; }
//    public bool A1_DoorStatus { get; set; }
//    public bool A2_DoorStatus { get; set; }
//    public bool B1_DoorStatus { get; set; }
//    public bool C1_DoorStatus { get; set; }
//    public bool Discounter_Reset { get; set; }
//    public int Discounter { get; set; }
//}
