using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.ServiceModel;
using System.Text;
using MPUListener;
using Newtonsoft.Json;
using Windows.Services.Maps;

namespace M3_Ybs.BackGroundServices
{
    public class Manager : BackgroundService
    {


        private readonly HttpClient _client;

        private readonly JsonController _jsonController;

        public Stations currentStation;//Suanki istasyon
        public Stations nextStation;   //Bir sonraki istasyon
        public int takoValue;          //tako degeri


        public static bool AnonsCounter = false;
        public static bool AnonsCounter2 = false;

        public static List<Stations> stationData;
        public static bool IsConnected = false;//TCMS baglantı kontrolü

        public static bool IsSendAnonsPost = false;//Anons Post metodu bir kez caldırma icin kontrol


        public static bool isLastStationReached = false; // Son istasyon  Kontrol değişkeni


        public static string selectedMesafe;  //Bir sonraki istasyon ile olan mesafesi
        public static string selectedBoy;   //İstasyon kendi boyu


        public static readonly int MaxRetryCount = 3;  // TCMS ile baglantı kontrolü icin
        public static readonly int RetryInterval = 1000;


        public string takoKatSayisi { get; set; } //Metro tker yarıcapı

        public string istasyondanCıkısMesafesi { get; set; } // İstasyondan cıkıs Anons mesafesi
        public string istasyonaKalanMesafe { get; set; }   // İstasyona yaklasım Anons mesafesi


        private static MPU m_mpuServiceMPU;



        //GlobalVariablesDTO sınıfında tanımlamıs oldugumuz degiskenle Mpu dan aldıgımız bütün verileri kendi Global degiskenlerimize esitledik
        public Manager(JsonController jsonController)
        {
            _jsonController = jsonController;

            ReadJsonTakoValue();// tako kat sayısı ve mesafeleri almak icin

            StartMPUServiceClient();//Baslarken MPU ile baglantı kuran metot

            GlobalVariablesDTO.m_client.ResetDistanceCounterAsync(true); // Baslar baslamaz tako reset istegi gönderiyoruz


        }

        #region   WCF Servisi ile baglantı kuran ilk metot
        public static async Task StartMPUServiceClient() // MPU  server ile baglantı baslatma
        {


            try
            {
                if (GlobalVariablesDTO.m_client == null)
                {
                    InstanceContext context = new InstanceContext(new TCMSConnectionService());


                    GlobalVariablesDTO.m_client = new MPUListener.M3YBSCommunicationClient(context);


                    //GlobalVariablesDTO.m_client.InnerChannel.OperationTimeout = TimeSpan.MaxValue;//Baglantı oldugu sürece deavam eder


                    GlobalVariablesDTO.m_client?.SubscribeAsync();

                    IsConnected = true;


                }

            }
            catch (Exception ex)
            {
                // Handle connection error and initiate retry logic
                IsConnected = false;
                Debug.WriteLine("TCMS Server baglantısı YOOOOOK");

                // Implement retry mechanism here
                for (int i = 0; i < MaxRetryCount; i++)
                {
                    Debug.WriteLine("***********-----Yenidennnnnnnnn---------Baglandııı-----*******");

                    StartMPUServiceClient();

                    // Wait for retry interval
                    System.Threading.Thread.Sleep(RetryInterval);
                }

                // Connection could not be re-established
                if (!IsConnected)
                {
                    Debug.WriteLine("TCMS Server baglantısı yapıldı tekrar");
                }
            }
        }
        #endregion

        #region    BackgroundServis olarak ilk calısan Metot
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)// Backgroundservice ilk çalışan metodu
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                //await SendHttpPostRequest();  
                await Worker_DoWork();
                //await Task.Delay(10, stoppingToken);
            }
        }
        #endregion


        #region Jsondan Rota Okuma


        public static List<Stations> ReadJsonData() //Json dan veri okuma metodu
        {
            //SendHttpPostRequest();
            string jsonFilePath = "stationData.json";
            string jsonContent = File.ReadAllText(jsonFilePath);
            if (jsonContent != null)
            {

                GlobalVariablesDTO.StationList.AddRange(JsonConvert.DeserializeObject<List<Stations>>(jsonContent));

            }
            else
            {
                Debug.WriteLine("Dosya bulunamadııı");
            }

            return GlobalVariablesDTO.StationList;
        }
        #endregion





        #region Calısan İLK Fonksiyonumuz


        public async Task Worker_DoWork() //Sürekli calısan Worker metodu
        {
            //Debug.WriteLine("Tako", GlobalVariablesDTO.proxy.ICountDist());
            //Debug.WriteLine("A1", M3_Ybs.GlobalVariablesDTO.proxy.A1DoorStatus().ToString());
            await SendHttpPostRequest();
            GlobalVariablesDTO.StationList.Clear();
            //stationData.Clear();
            stationData = ReadJsonData();
            Stations.CurrentStation = stationData[0].istasyonAdi;




            //GlobalVariablesDTO.Discounter = int.Parse(GlobalVariablesDTO.Discounter.ToString()); //TCMS den gelen tako verisi
            try
            {


                int discounter;
                bool success = int.TryParse(GlobalVariablesDTO.Discounter.ToString(), out discounter); // TCMS den gelen Tako degeri gelince ata ve basla . 

                //if (!string.IsNullOrEmpty(GlobalVariablesDTO.Discounter.ToString()) && (GlobalVariablesDTO.Discounter > 0))

                if (success) // Tako degeri gelince baslasın
                {


                    GlobalVariablesDTO.Discounter = discounter;// Tako değeri bir sayı ise, değeri `GlobalVariablesDTO.Discounter` değişkenine atar.

                    for (int i = 0; i < stationData.Count - 1; i++)
                    {
                        Stations currentStation = stationData[i];//Suanki istasyon
                        Stations nextStation = stationData[i + 1];//Bir sonraki istasyon
                                                                  //Stations nextStation;//Bir sonraki istasyon

                        //Stations s = new Stations();

                        //s = stationData[i + 1];


                        //nextStation.id = s.id;
                        //nextStation.istasyonAdi = s.istasyonAdi;
                        //nextStation.istasyonBoyT1 = s.istasyonBoyT1;
                        //nextStation.istasyonBoyT2 = s.istasyonBoyT2;
                        //nextStation.istasyonMesafeT1 = s.istasyonMesafeT1;
                        //nextStation.istasyonMesafeT2 = s.istasyonMesafeT2;


                        Stations.CurrentStation = Stations.CurrentStation;

                        while (!IsAtNextStation(currentStation, nextStation, GlobalVariablesDTO.Discounter))
                        {
                            //Stations.CurrentStation = Stations.CurrentStation;
                            await SendHttpPostRequest();

                            try
                            {


                                //IsConnected = true;
                                Debug.WriteLine($"Suankiiii istasyonnnnnn: {currentStation.istasyonAdi}-------{nextStation.istasyonAdi}");

                                Debug.WriteLine("A1  kapı durumuuu   " + GlobalVariablesDTO.A1_DoorStatus, GlobalVariablesDTO.metroName);
                                // Tako değerini sürekli arttır
                                GlobalVariablesDTO.Discounter = int.Parse(GlobalVariablesDTO.Discounter.ToString()); // Tako dan gelen değeri alıyor sürekli


                                if (Stations.IsRouteChanged == true)
                                {
                                    Debug.WriteLine("**************Rota güncellendi ve yeni rota kuruldu*****************");
                                    GlobalVariablesDTO.StationList.Clear();
                                    stationData.Clear();//Eger rota yeniden kurulmussa  Liste temizleniyor yenisi okunuyor

                                    //Tako degerini sıfırla
                                    GlobalVariablesDTO.m_client.ResetDistanceCounterAsync(true);
                                    GlobalVariablesDTO.Discounter = 0;


                                    GlobalVariablesDTO.sendReaminingDistance = 0;//Kalan mesafe sıfırlanır

                                    await SendHttpPostRequest();
                                    stationData = ReadJsonData();//Yeni rotayı oku


                                    currentStation = stationData[i];//Suanki istasyon
                                    nextStation = stationData[i + 1];//Bir sonraki istasyon

                                    Stations.CurrentStation = currentStation.istasyonAdi;//Suan ki istasyon adı atanır



                                }

                                //TCMS ile baglantı kopması durumunda calısıcak 
                                CheckConnection();


                            }
                            catch (Exception ex)
                            {
                                Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "281 Nolu satır hataya düstü");
                                Debug.WriteLine("Hataya  düsttüüüüüü");
                                StartMPUServiceClient();


                            }
                        }

                    }

                }
                else // Tako degeri gelmedigi sürece bekle
                {
                    Logging.WriteLog(DateTime.Now.ToString(), "ex.Message", "ex.StackTrace", "ex.TargetSite.ToString()", "Tako degeri gelmedigi icin bekliyor");
                    Debug.WriteLine("Tako degeri null veya 0 ! Lütfen TCMS baglantısını kontrol edin");// Tako değeri bir sayı değilse, `GlobalVariablesDTO.Discounter` değişkeninin değerini değiştirmez.

                    //while (GlobalVariablesDTO.Discounter > 0)
                    //{
                    //    // Tako degeri gelene kadar bekliyoruz
                    //    Thread.Sleep(1000); // 1 saniye bekleyin 



                    //    //Eger bu sürede TCMS ile baglantı sorunu olursa tekrar baglantı kuralım

                    CheckConnection();


                    //}

                    Debug.WriteLine("Bekleme süresi bitti, devam ediliyor.");



                }
            }
            catch (Exception ex)
            {


                Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "Tcms ile Ag baglantısını kotnrol edin");

                while (!IsConnected)
                {

                    Debug.WriteLine("MPU ile baglntı kop tu tekrar baglanmaya calısılıyor!!!");

                    StartMPUServiceClient();
                    Debug.WriteLine("\"Tcms ile Ag baglantısını kotnrol edin\"");


                    CheckConnection();

                }
            }
        }

        #endregion



        #region   Hesaplama yapan Algoritma Fonksiyonumuz


        static bool IsAtNextStation(Stations currentStation, Stations nextStation, int takoValue)
        {
            try
            {



                // Rota türüne göre T1 veya T2 değerlerini seçme işlemi
                takoValue = 0;

                int currentStationIndex = GlobalVariablesDTO.StationList.FindIndex(station => station.id == currentStation.id);
                int nextStationIndex = currentStationIndex + 1;

                if (nextStationIndex < GlobalVariablesDTO.StationList.Count)
                {
                    int currentId = Int32.Parse(GlobalVariablesDTO.StationList[currentStationIndex].id);
                    int nextId = Int32.Parse(GlobalVariablesDTO.StationList[nextStationIndex].id);

                    if (currentId < nextId)   //ilk istasyonun id si sonraki istasyonun id sinden kücük ise T1 leri alıyoruz
                    {
                        selectedMesafe = currentStation.istasyonMesafeT1;
                        selectedBoy = currentStation.istasyonBoyT1;
                    }
                    else  //ilk istasyonun id si sonraki istasyonun id sinden büyük ise T2 leri  alıyoruz
                    {
                        selectedMesafe = currentStation.istasyonMesafeT2;
                        selectedBoy = currentStation.istasyonBoyT2;
                    }
                }

                // Her bir istasyon arasındaki mesafeyi kontrol edelim
                //int currentDistance = int.Parse(currentStation.istasyonMesafeT1);
                //int nextDistance = int.Parse(nextStation.istasyonBoyT1);

                int currentDistance = int.Parse(selectedMesafe);
                int nextDistance = int.Parse(selectedBoy);

                int sumDistance = currentDistance + nextDistance;
                takoValue = GlobalVariablesDTO.Discounter;


                // Bir sonraki istasyona kadar kalan mesafe
                //int remainingDistance = (currentDistance + nextDistance) - (takoValue); // Bir sonraki istasyon mesafesi ve aradaki mesafenin toplamından tako degerini çıkarıyoruz

                //int remainingDistance = Math.Max((sumDistance) - (takoValue), 0);
                //int stationDistance = Math.Max((nextDistance) - (takoValue), 0); //İstasyon mesafesinden tako degerini çıkarıp anonsu nerede yapacagını hesaplamak icin
                //int stationDistance = nextDistance - takoValue;

                int remainingDistance = (sumDistance) - (takoValue);
                int stationDistance = (nextDistance) - (takoValue);


                GlobalVariablesDTO.sendReaminingDistance = remainingDistance;//Apı ile Makiniste gönderilen kalan mesafe
                GlobalVariablesDTO.sendDistance = sumDistance;//Apı ile Makiniste gönderilen toplam mesafe


                if ((80 < stationDistance) && (stationDistance <= (nextDistance - GlobalVariablesDTO.istasyondanCıkısMesafesi)))//istasyondan çıktıktan 10 metre sonra
                {

                    Stations.CurrentStation = nextStation.istasyonAdi;

                    if (!AnonsCounter)
                    {
                        AnonsCounter = true;
                  

                        MPUListener.AnnouncementDTO announcementDTO = new MPUListener.AnnouncementDTO();

                        if (GlobalVariablesDTO.metroName=="M3")
                            announcementDTO.metroLines = EnumsMetroLines.M3;
                        else
                            announcementDTO.metroLines = EnumsMetroLines.M9;

                        announcementDTO.status = EnumsAnnouncement.Play;
                        announcementDTO.announcementType = EnumsAnnouncementType.Approach;
                        announcementDTO.stationName = ConvertStationNameStringToEnum(Stations.CurrentStation);

                        GlobalVariablesDTO.m_client.AnnouncementStatusAsync(announcementDTO);

                    }


                    //Debug.WriteLine($"Gelecek İstasyon Anonsu Yapılıyorrr :{nextStation.istasyonAdi}");
                    




                    if (!IsSendAnonsPost)
                    {


                        Debug.WriteLine("Yaklasımmmmm ----- Anonsumuzzzzzz", GlobalVariablesDTO.metroName, Stations.CurrentStation, "İkinci   anonsuuu", Stations.CurrentStation.ToString());

                        //SendHttpPostAnons(GlobalVariablesDTO.metroName, "Station", Stations.CurrentStation, "yaklasim.wav");
                        //SendHttpPostAnons("M3", "Station", Stations.CurrentStation, "yaklasim.wav");

                        IsSendAnonsPost = true;

                    }

                    IsSendAnonsPost = false;
                    //AnonsCounter = false;
                }

                else if ((60 < remainingDistance) && (remainingDistance <= GlobalVariablesDTO.istasyonaKalanMesafe))//istasyondan cıktıktan 200 metre sonra gelecek istasyon anonsu
                {

                    Stations.CurrentStation = Stations.CurrentStation;
                    //Debug.WriteLine($"İstasyona   Geliyoruzzzz :{remainingDistance},İstasyon Adııı :{currentStation.istasyonAdi}");

                    //Debug.WriteLine($"Varısss Anonsu Yapılıyorrr :{currentStation.istasyonAdi}");
                    //IsSendAnonsPost = false;



                    if (!AnonsCounter2)
                    {
                        AnonsCounter2 = true;
                      
                        //GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);

                        MPUListener.AnnouncementDTO announcementDTO = new MPUListener.AnnouncementDTO();

                        if (GlobalVariablesDTO.metroName == "M3")
                            announcementDTO.metroLines = EnumsMetroLines.M3;
                        else
                            announcementDTO.metroLines = EnumsMetroLines.M9;

                        announcementDTO.status = EnumsAnnouncement.Play;
                        announcementDTO.announcementType = EnumsAnnouncementType.Station;
                        announcementDTO.stationName = ConvertStationNameStringToEnum(Stations.CurrentStation);

                        GlobalVariablesDTO.m_client.AnnouncementStatusAsync(announcementDTO);

                    }


                    if (!IsSendAnonsPost)
                    {


                        Debug.WriteLine("Varısssss   ------  Anonsumuzzzzzz", GlobalVariablesDTO.metroName, Stations.CurrentStation, "İkinci   anonsuuu", Stations.CurrentStation.ToString());

                        //SendHttpPostAnons(GlobalVariablesDTO.metroName, "Station", Stations.CurrentStation, "varis.wav");
                        //SendHttpPostAnons("M3", "Station", Stations.CurrentStation, "varis.wav");


                        IsSendAnonsPost = true;

                    }
                    //Thread.Sleep(5000);
                    IsSendAnonsPost = false;
                    //AnonsCounter2 = false;

                }

                else if ((remainingDistance <= 50))
                {

                    IsSendAnonsPost = true;
                    AnonsCounter = false;
                    AnonsCounter2 = false;
 
                    if ((M3_Ybs.GlobalVariablesDTO.TrenSpeed < 30) && (M3_Ybs.GlobalVariablesDTO.All_LeftDoor_Release == true || M3_Ybs.GlobalVariablesDTO.All_RightDoor_Release == true) && (M3_Ybs.GlobalVariablesDTO.A1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.A2_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.B1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.C1_DoorStatus == true))
                    {
                        //ResetTakoValue();
                        Task.Run(async () =>
                        {
                            await GlobalVariablesDTO.m_client.ResetDistanceCounterAsync(true);
                            // Buraya gelen kod, AnnouncementStatusAsync tamamlandığında devam eder.
                        }).Wait();

                        GlobalVariablesDTO.Discounter = 0;

                        Debug.WriteLine($"Tako değeri sıfırlandı: {GlobalVariablesDTO.Discounter}");


                        // Burada şuanki istasyonu bir sonraki istasyon olarak güncelle
                        Stations.CurrentStation = nextStation.istasyonAdi;

                        // nextStation'ı güncelleyelim (nextStation'dan bir sonraki istasyon)

                        currentStation.istasyonAdi = nextStation.istasyonAdi;
                        currentStation.istasyonBoyT1 = nextStation.istasyonBoyT1;
                        currentStation.istasyonBoyT2 = nextStation.istasyonBoyT2;
                        currentStation.id = nextStation.id;
                        currentStation.istasyonMesafeT1 = (int.Parse(nextStation.istasyonMesafeT1)+remainingDistance).ToString();
                        currentStation.istasyonMesafeT2 = (int.Parse(nextStation.istasyonMesafeT2) + remainingDistance).ToString();


                        Debug.WriteLine(currentStation.istasyonMesafeT1, currentStation.istasyonMesafeT2);


                        int nextStationIndexx = GlobalVariablesDTO.StationList.FindLastIndex(station => station.istasyonAdi == currentStation.istasyonAdi) + 1;

                        if (nextStationIndexx < GlobalVariablesDTO.StationList.Count)
                        {
                            Stations s = new Stations();

                            s = GlobalVariablesDTO.StationList[nextStationIndexx];


                            nextStation.id = s.id;
                            nextStation.istasyonAdi = s.istasyonAdi;
                            nextStation.istasyonBoyT1 = s.istasyonBoyT1;
                            nextStation.istasyonBoyT2 = s.istasyonBoyT2;
                           
                            nextStation.istasyonMesafeT1 =s.istasyonMesafeT1;
                            nextStation.istasyonMesafeT2 =s.istasyonMesafeT2;



                            

                            Debug.WriteLine("***********---------Sonraki duraga gidicekkk----------***************");
                            IsAtNextStation(currentStation, nextStation, takoValue);

                        }

                        else
                        {
                            // Eğer bir sonraki istasyon yoksa, burada gerekli işlemleri gerçekleştirebilirsiniz.
                            // Örneğin, rota sona erdiği için bir başka rota kurma veya ilgili değişkenleri sıfırlama gibi.
                            var lastItem = GlobalVariablesDTO.StationList.Last();//Listenin en son elemanı

                            if (Stations.CurrentStation == lastItem.istasyonAdi)
                            {
                                // Son istasyona gelindiğinde ilgili işlemleri yapabilirsiniz.
                                Debug.WriteLine("Rotalar bitttttiiiiiiiii");

                                GlobalVariablesDTO.StationList.Clear();
                                stationData.Clear();
                                Debug.WriteLine("Kurulan Rotalar Temizlendiiiii---- Yeni Rota bekleniyorr");

                                GlobalVariablesDTO.Discounter = 0;
                                isLastStationReached = true; // Son istasyona ulaşıldığını işaretle
                                while (Stations.IsRouteChanged == false)
                                {
                                    Thread.Sleep(1000);// Yeni rota kurulana kadar bekle
                                    Debug.WriteLine("***YENİ ROTA BEKLENİYOR***");
                                    SendHttpPostRequest();//tekrar rotayı kontrol ett


                                    //Eger bu sürede TCMS ile baglantı sorunu olursa tekrar baglantı kuralım
                                    CheckConnection();
                                


                                }

                               
                            }

                            GlobalVariablesDTO.StationList.Clear();
                            stationData.Clear();
                            stationData = ReadJsonData();//Yeni rotayı oku

                            IsAtNextStation(currentStation, nextStation, takoValue);

                        }
                       

                    }



                    //IsAtNextStation(currentStation, nextStation, takoValue);

                    while (remainingDistance <= 0)
                    {
                        Debug.WriteLine($"Tako değeri bekleniyor :{takoValue}");
                        // Tako değeri gelir gelmez işlemleri başlat
                        if (!string.IsNullOrEmpty(GlobalVariablesDTO.Discounter.ToString()))
                        {
                            Debug.WriteLine($"Tako değeri geldi :{GlobalVariablesDTO.Discounter.ToString()}");
                           

                            break;
                        }

                    }

                }

                SendHttpPostRequest();//tekrar rotayı kontrol ett


                Debug.WriteLine($"Gelen TAko Degerii------- :{GlobalVariablesDTO.Discounter.ToString()}");
                Debug.WriteLine($"Distancee degeri :{remainingDistance}");
                Debug.WriteLine("Trenin Hızı ---->" + M3_Ybs.GlobalVariablesDTO.TrenSpeed);
                //IsConnectedTcms = true;


                return remainingDistance == 0;
            }
            catch (CommunicationException ex)
            {
                Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "***556 nolu satır bir hata ile karsılastı!!!");
                Debug.WriteLine("------*****Bir hata ile karşılaştı\n Tekrar baglantı kontrolleri yapılıyor-----*****");

                StartMPUServiceClient();
            }
            return true;
        }



        #endregion




        //TCMS ile baglantı kontrolünü saglayan metot
        #region  TCMS ile baglantı kontrolünü yapan metot
        private static void CheckConnection()
        {
            if (GlobalVariablesDTO.m_client.State == CommunicationState.Closed || GlobalVariablesDTO.m_client.State == CommunicationState.Faulted)
            {
                Logging.WriteLog(DateTime.Now.ToString(), "Message", "StackTrace", "TargetSite.ToString()", "Tcms ile Ag baglantı koptu Tekrar baglanmayacalsıyor");
                Debug.WriteLine("***********---------HATATTTTATAAAAAAAA----------***************");
                //Thread.Sleep(1000);
                Debug.WriteLine("***********---------TEKRAR BAGLANMAYA CALISIYOR----------***************");
                GlobalVariablesDTO.m_client.UnsubscribeAsync();
                GlobalVariablesDTO.m_client.Abort();


                InstanceContext context = new InstanceContext(new TCMSConnectionService());

                //GlobalVariablesDTO.m_client.InnerChannel.OperationTimeout = TimeSpan.MaxValue;//Baglantı oldugu sürece devam eder

                GlobalVariablesDTO.m_client = new MPUListener.M3YBSCommunicationClient(context);

                GlobalVariablesDTO.m_client?.SubscribeAsync();

                IsConnected = true;

                Debug.WriteLine("***********---------TEKRAR BAGLANTI SAGLANDI----------***************");

            }
        }

        #endregion

        #region   Tako degeri Resetleyen fonksiyon
        public static void ResetTakoValue()
        {
            // Tako değerini sıfırla (örneğin, 0 yap)

            GlobalVariablesDTO.m_client.ResetDistanceCounterAsync(true);

            GlobalVariablesDTO.Discounter = 0;
            Debug.WriteLine($"Tako değeri sıfırlandı: {GlobalVariablesDTO.Discounter}");


        }
        #endregion



        #region Güncel rota kurulup kurulmadıgını Kontrol eden Post servisimiz


        static async Task SendHttpPostRequest()
        {
            try
            {
                // SSL hatalarını kontrol etmeksizin bir HTTP isteği yapmak için kullanılır.
                //ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                // HTTP POST isteği için gerekli ayarlamaları yapın


                string apiUrl = "https://10.3.156.70:7042/api/Json/GetData";


                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                HttpClient client = new HttpClient(handler);

                // HTTP POST isteği gönderin
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                // Yanıtı kontrol edin
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("HTTP GET isteği başarıyla gönderildi.");
                }
                else
                {
                    Debug.WriteLine($"HTTP GET isteği başarısız: {response.StatusCode}");
                }

            }
            catch (HttpRequestException ex)
            {
                Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "***Rota Almak icin HTTP Post sırasında hata olustu");
                Debug.WriteLine($"***Rota Almak icin HTTP POST isteği sırasında bir hata oluştu: {ex.Message}");
            }
            finally
            {
                // SSL hatalarını kontrol etmeksizin bir HTTP isteği yapmak için kullanılan callback'i temizler.
                ServicePointManager.ServerCertificateValidationCallback -= (sender, cert, chain, sslPolicyErrors) => true;
            }
        }


        #endregion




        #region Anons yapan Post Servisimiz


        static async Task SendHttpPostAnons(string metro, string pathName, string stationName, string soundName) //Anons caldırmak icin POST fonksiyonu
        {
            try
            {
                // SSL hatalarını kontrol etmeksizin bir HTTP isteği yapmak için kullanılır.
                //ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


                string apiUrl = "https://10.3.156.130:7069/api/Home/SoundPlay"; //Adalet Ses dosyası caldırmak icin

                metro = GlobalVariablesDTO.metroName;

                Sound data = new() { metroName = metro, pathName = pathName, stationName = stationName, soundName = soundName };

                var content = new StringContent($"{data}", Encoding.UTF8, "application/json");


                Debug.WriteLine("Alınan  içerik: " + await content.ReadAsStringAsync());

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                HttpClient client = new HttpClient(handler);

                // HTTP POST isteği gönderin
                HttpResponseMessage response = await client.PostAsJsonAsync(apiUrl, data);

                // Yanıtı kontrol edin
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Anons icin HTTP POST isteği başarıyla gönderildi.");
                }
                else
                {
                    Debug.WriteLine($"Anons icin HTTP POST isteği başarısız: {response.StatusCode}");
                }

            }
            catch (HttpRequestException ex)
            {
                Logging.WriteLog(DateTime.Now.ToString(), ex.Message, ex.StackTrace, ex.TargetSite.ToString(), "***Anons icin HTTP Post sırasında hata olustu");
                Debug.WriteLine($"***Anons icin HTTP POST isteği sırasında bir hata oluştu: {ex.Message}");
            }
            finally
            {
                // SSL hatalarını kontrol etmeksizin bir HTTP isteği yapmak için kullanılan callback'i temizler.
                ServicePointManager.ServerCertificateValidationCallback -= (sender, cert, chain, sslPolicyErrors) => true;
            }
        }
        #endregion



        #region   Parametrik mesafeleri Json dan okuyan metot
        public static void ReadJsonTakoValue() //Json dan veri okuma metodu
        {
            //SendHttpPostRequest();
            string jsonFilePath = "takoValue.json";
            string jsonContent = File.ReadAllText(jsonFilePath);


            dynamic array = JsonConvert.DeserializeObject(jsonContent);
            foreach (var jsonObject in array)
            {
                string takoKatSayisiDegeri = jsonObject["takoKatSayisi"].ToString();
                string istasyondanCıkısMesafesiDegeri = jsonObject["istasyondanCıkısMesafesi"].ToString();
                string istasyonaKalanMesafeDegeri = jsonObject["istasyonaKalanMesafe"].ToString();

                // Değeri bir değişkene atayın
                GlobalVariablesDTO.takoKatSayisi = double.Parse(takoKatSayisiDegeri, NumberStyles.Any, CultureInfo.InvariantCulture);
                GlobalVariablesDTO.istasyondanCıkısMesafesi = int.Parse(istasyondanCıkısMesafesiDegeri);
                GlobalVariablesDTO.istasyonaKalanMesafe = int.Parse(istasyonaKalanMesafeDegeri);
            }
        }
        #endregion



        #region   İstasyon isimlerini dönüstüren metot
        public static MPUListener.EnumsStationName ConvertStationNameStringToEnum(string stationName)
        {
            MPUListener.EnumsStationName stationNameEnum = MPUListener.EnumsStationName.İlkyuva;

            switch (stationName)
            {
                case "Kayaşehir Merkez":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.KayaşehirMerkez;
                        break;
                    }
                case "Toplu Konutlar":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.TopluKonutlar;
                        break;
                    }
                case "Şehir Hastanesi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.ŞehirHastanesi;
                        break;
                    }
                case "Onurkent":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Onurkent;
                        break;
                    }
                case "Başakşehir - Metrokent":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.BaşakşehirMetrokent;
                        break;
                    }
                case "Başak Konutları":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.BaşakKonutları;
                        break;
                    }
                case "Siteler":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Siteler;
                        break;
                    }
                case "Turgut Özal":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.TurgutÖzal;
                        break;
                    }
                case "İkitelli Sanayi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.İkitelliSanayi;
                        break;
                    }
                case "İSTOÇ":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.İSTOÇ;
                        break;
                    }
                case "Mahmutbey":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Mahmutbey;
                        break;
                    }
                case "Yenimahalle":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Yenimahalle;
                        break;
                    }
                case "Kirazlı - Bağcılar":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Kirazlı;
                        break;
                    }
                case "Molla Gürani":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.MollaGürani;
                        break;
                    }
                case "Yıldıztepe":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Yıldıztepe;
                        break;
                    }
                case "İlkyuva":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.İlkyuva;
                        break;
                    }
                case "Haznedar":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Haznedar;
                        break;
                    }
                case "Bakırköy - İncirli":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Bakırköyİncirli;
                        break;
                    }
                case "Özgürlük Meydanı":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.ÖzgürlükMeydanı;
                        break;
                    }
                case "Bakırköy İDO":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.BakırköyİDO;
                        break;
                    }
                case "Ataköy":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Ataköy;
                        break;
                    }
                case "Yenibosna":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Yenibosna;
                        break;
                    }
                case "ÇobançeşmeKuyumcukent":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.ÇobançeşmeKuyumcukent;
                        break;
                    }
                case "İhlasYuva":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.İhlasYuva;
                        break;
                    }
                case "DoğuSanayi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.DoğuSanayi;
                        break;
                    }
                case "MimarSinan":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.MimarSinan;
                        break;
                    }
                case "OnBeşTemmuz":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.OnBeşTemmuz;
                        break;
                    }
                case "HalkalıCaddesi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.HalkalıCaddesi;
                        break;
                    }
                case "AtatürkMahallesi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.AtatürkMahallesi;
                        break;
                    }
                case "Bahariye":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Bahariye;
                        break;
                    }

                case "MASKO":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.MASKO;
                        break;
                    }

                case "ZiyaGökalpMahallesi":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.ZiyaGökalpMahallesi;
                        break;
                    }
                case "Olimpiyat":
                    {
                        stationNameEnum = MPUListener.EnumsStationName.Olimpiyat;
                        break;
                    }
            }

            return stationNameEnum;
        }

        #endregion

    }
}
