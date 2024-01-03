using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.ServiceModel;
using System.Text;
using MPUListener;
using Newtonsoft.Json;

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


                    if (CheckConnection())
                    {
                        Debug.WriteLine("Tcms ile Ag baglantısı yeniden sağlandı.");
                        break;
                    }

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

                int remainingDistance = Math.Max((sumDistance) - (takoValue), 0);
                int stationDistance = Math.Max((nextDistance) - (takoValue), 0); //İstasyon mesafesinden tako degerini çıkarıp anonsu nerede yapacagını hesaplamak icin
                                                                                 //int stationDistance = nextDistance - takoValue;


                GlobalVariablesDTO.sendReaminingDistance = remainingDistance;//Apı ile Makiniste gönderilen kalan mesafe
                GlobalVariablesDTO.sendDistance = sumDistance;//Apı ile Makiniste gönderilen toplam mesafe


                if ((80 < stationDistance) && (stationDistance <= GlobalVariablesDTO.istasyondanCıkısMesafesi))//istasyondan çıktıktan 10 metre sonra
                {



                    if (!AnonsCounter)
                    {
                        AnonsCounter = true;
                        //Task lele = GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);//MPU ya ses kanalı acması icin 
                        //lele.Wait();
                        //Task.Run(async () =>
                        //{
                        //    await GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);
                        //    // Buraya gelen kod, AnnouncementStatusAsync tamamlandığında devam eder.
                        //}).Wait();
                        GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);

                    }


                    //Debug.WriteLine($"Gelecek İstasyon Anonsu Yapılıyorrr :{nextStation.istasyonAdi}");
                    Stations.CurrentStation = nextStation.istasyonAdi;




                    if (!IsSendAnonsPost)
                    {


                        Debug.WriteLine("Yaklasımmmmm ----- Anonsumuzzzzzz", GlobalVariablesDTO.metroName, Stations.CurrentStation, "İkinci   anonsuuu", Stations.CurrentStation.ToString());

                        SendHttpPostAnons(GlobalVariablesDTO.metroName, "Station", Stations.CurrentStation, "yaklasim.wav");
                        //SendHttpPostAnons("M3", "Station", Stations.CurrentStation, "yaklasim.wav");

                        IsSendAnonsPost = true;

                    }

                    IsSendAnonsPost = false;
                    //AnonsCounter = false;
                }

                else if ((50 < remainingDistance) && (remainingDistance <= GlobalVariablesDTO.istasyonaKalanMesafe))//istasyondan cıktıktan 200 metre sonra gelecek istasyon anonsu
                {

                    Stations.CurrentStation = Stations.CurrentStation;
                    //Debug.WriteLine($"İstasyona   Geliyoruzzzz :{remainingDistance},İstasyon Adııı :{currentStation.istasyonAdi}");

                    //Debug.WriteLine($"Varısss Anonsu Yapılıyorrr :{currentStation.istasyonAdi}");
                    //IsSendAnonsPost = false;



                    if (!AnonsCounter2)
                    {
                        AnonsCounter2 = true;
                        //Task lolo = GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);//MPU ya ses kanalı acması icin 
                        //lolo.Wait();

                        //Task.Run(async () =>
                        //{
                        //    await GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);
                        //    // Buraya gelen kod, AnnouncementStatusAsync tamamlandığında devam eder.
                        //}).Wait();
                        GlobalVariablesDTO.m_client.AnnouncementStatusAsync(EnumsAnnouncement.Play);

                    }


                    if (!IsSendAnonsPost)
                    {


                        Debug.WriteLine("Varısssss   ------  Anonsumuzzzzzz", GlobalVariablesDTO.metroName, Stations.CurrentStation, "İkinci   anonsuuu", Stations.CurrentStation.ToString());

                        SendHttpPostAnons(GlobalVariablesDTO.metroName, "Station", Stations.CurrentStation, "varis.wav");
                        //SendHttpPostAnons("M3", "Station", Stations.CurrentStation, "varis.wav");


                        IsSendAnonsPost = true;

                    }
                    //Thread.Sleep(5000);
                    IsSendAnonsPost = false;
                    //AnonsCounter2 = false;

                }
                else if (remainingDistance <= 15)
                {

                    IsSendAnonsPost = true;
                    AnonsCounter = false;
                    AnonsCounter2 = false;
                    //Debug.WriteLine($"İstasyona   geldikk :{remainingDistance},İstasyon Adııı :{currentStation.istasyonAdi}");

                    //GlobalVariablesDTO.m_client.ResetDistanceCounterAsync(true);

                    //GlobalVariablesDTO.Discounter = 0;
                    //await SendHttpPostRequest(currentStation.istasyonAdi);

                    //Debug.WriteLine("Trenin Hızı ---->" + M3_Ybs.GlobalVariablesDTO.TrenSpeed);
                    //Debug.WriteLine("Sol Kapıların Release Durumu ---->" + M3_Ybs.GlobalVariablesDTO.All_LeftDoor_Release);
                    //Debug.WriteLine("Sağ Kapıların Release Durumu ---->" + M3_Ybs.GlobalVariablesDTO.All_RightDoor_Release);
                    //Debug.WriteLine("A1 Kapısı Durumu ---->" + M3_Ybs.GlobalVariablesDTO.A1_DoorStatus);
                    //Debug.WriteLine("A2 Kapısı Durumu ---->" + M3_Ybs.GlobalVariablesDTO.A2_DoorStatus);
                    //Debug.WriteLine("B1 Kapısı Durumu ---->" + M3_Ybs.GlobalVariablesDTO.B1_DoorStatus);
                    //Debug.WriteLine("C1 Kapısı Durumu ---->" + M3_Ybs.GlobalVariablesDTO.C1_DoorStatus);

                    //Burada Tren hızının 3km/s altında ve Kapıların Release ve A1,A2,B1,C1 kapılarının acık kapalı durumlarının kontrolü yapılıcaktır.
                    //if (M3_Ybs.GlobalVariablesDTO.TrenSpeed < 30 && M3_Ybs.GlobalVariablesDTO.All_LeftDoor_Release == true || M3_Ybs.GlobalVariablesDTO.All_RightDoor_Release == true && M3_Ybs.GlobalVariablesDTO.A1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.A2_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.B1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.C1_DoorStatus == true)
                    //{
                    //    ResetTakoValue();// Burada tako degeri sıfırlanıyor. TCMS  e burda tako sıfırla tetigi göndermek gerekiyor

                    //}

                    //Depo sahasında test icin

                    //if (M3_Ybs.GlobalVariablesDTO.A1_DoorStatus == true)
                    //{ 
                    if (M3_Ybs.GlobalVariablesDTO.TrenSpeed < 30 && M3_Ybs.GlobalVariablesDTO.All_LeftDoor_Release == true || M3_Ybs.GlobalVariablesDTO.All_RightDoor_Release == true && M3_Ybs.GlobalVariablesDTO.A1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.A2_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.B1_DoorStatus == true || M3_Ybs.GlobalVariablesDTO.C1_DoorStatus == true)
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
                        //Stations.CurrentStation = nextStation.istasyonAdi;

                        ////currentStation = Stations.CurrentStation;

                        //IsAtNextStation(currentStation, nextStation, takoValue);
                        var lastItem = GlobalVariablesDTO.StationList.Last();//Listenin en son elemanı

                        if (Stations.CurrentStation == lastItem.istasyonAdi)
                        {
                            // Son istasyona gelindiğinde ilgili işlemleri yapabilirsiniz.
                            Debug.WriteLine("Rotalar bitttttiiiiiiiii");
                            //Stations.CurrentStation = "SON DURAK";
                            //GlobalVariablesDTO.StationList.ForEach(station => station.istasyonAdi = "SON DURAK"); // Tüm istasyon adlarını temizle
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


                        }
                        else
                        {
                            Stations.CurrentStation = nextStation.istasyonAdi;

                            //currentStation = nextStation;

                            Debug.WriteLine("***********---------Sonraki duraga gidicekkk----------***************");
                            IsAtNextStation(currentStation, nextStation, takoValue);
                        }
                        //SendHttpPostRequest();//tekrar rotayı kontrol ett

                    }



                    //IsAtNextStation(currentStation, nextStation, takoValue);

                    while (remainingDistance <= 0)
                    {
                        Debug.WriteLine($"Tako değeri bekleniyor :{takoValue}");
                        // Tako değeri gelir gelmez işlemleri başlat
                        if (!string.IsNullOrEmpty(GlobalVariablesDTO.Discounter.ToString()))
                        {
                            Debug.WriteLine($"Tako değeri geldi :{GlobalVariablesDTO.Discounter.ToString()}");
                            //remainingDistance = ((currentDistance + nextDistance) - (GlobalVariablesDTO.proxy.ICountDist()));

                            break;
                        }

                    }

                }



                Debug.WriteLine($"Gelen TAko Degerii------- :{GlobalVariablesDTO.Discounter.ToString()}");
                Debug.WriteLine($"Distancee degeri :{remainingDistance}");
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





        private bool CheckConnection()
        {//TCMS ile baglantı kontrolünü saglayan metot
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync("net.tcp://192.168.1.5:8733/M3YBSCommunication/M3YBSCommunication");
                    return response.IsCompleted;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

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


    }
}
