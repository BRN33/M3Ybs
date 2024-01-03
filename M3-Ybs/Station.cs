using Newtonsoft.Json;

namespace M3_Ybs
{

    [JsonObject]
    public class Stations
    {
        //public  static Stations CurrentStation;

        //public Stations()
        //{
        //    CurrentStation = this;

        //}

        public static string CurrentStation { get;  set; }// Manager da currentstation değişkenine Controller da erişmek icin

        public static bool IsRouteChanged { get; set; }
        public string id { get; set; }
        public string istasyonAdi { get; set; }
        public string istasyonMesafeT2 { get; set; }
        public string istasyonMesafeT1 { get; set; }
        public string istasyonBoyT1 { get; set; }
        public string istasyonBoyT2 { get; set; }

    }

}
