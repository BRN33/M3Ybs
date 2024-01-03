

namespace M3_Ybs
{
    public class GlobalVariablesDTO
    {
        public static bool LeftDoor_Status { get; set; }  // Aracı sol kapıları durumu
        public static bool RightDoor_Status { get; set; } // Aracın sağ kapıları durumu

        public static bool All_LeftDoor_Release { get; set; } // Bütün sol kapıların serbest durumu
        public static bool All_RightDoor_Release { get; set; } // Bütün sağ kapıların serbest durumu

        public static bool A1_DoorStatus { get; set; } // A1 aracı kapıları durumu
        public static bool A2_DoorStatus { get; set; } // A2 aracı kapıları durumu
        public static bool B1_DoorStatus { get; set; } // B1 aracı kapıları durumu
        public static bool C1_DoorStatus { get; set; } // C1 aracı kapıları durumu

        public static bool Discounter_Reset { get; set; } // Tako degeri resetlenmesi
        public static int Discounter { get; set; }// Tako degeri 

        public static int TrenSpeed { get; set; }// Tren Hızı 



        // Global listeyi tanımla
        public static List<Stations> StationList { get; set; } = new List<Stations>();


        public static TCMSConnectionService proxy;

        public static MPUListener.M3YBSCommunicationClient m_client;
        public static MPUListener.MPU m_mpuServiceMPU;


        public static string metroName { get; set; }// Anons icin metro hattı ismi 

        public static int sendReaminingDistance { get; set; }// Makinist ekranına gönderilen kalan mesafe degeri 

        public static int sendDistance { get; set; }// Makinist ekranına gönderilen kalan mesafe degeri 


        public static double takoKatSayisi { get; set; } //Metro tker yarıcapı


        public static int istasyondanCıkısMesafesi { get; set; } // İstasyondan cıkıs Anons mesafesi
        public static int istasyonaKalanMesafe { get; set; }   // İstasyona yaklasım Anons mesafesi


    }
}
