using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using mpu;
using System.Diagnostics;
using System.Net;
//using MPU_Received;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }




    public static void StartAsync()
    {
        // ConfigurationBuilder olu�tur
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        //Console.WriteLine("Uygulama baslad��");

        // ServiceSettings b�l�m�ndeki ServiceAddress de�erini al
        var address = configuration.GetSection("ServiceSettings")["ServiceAddress"];

        //string address = "net.tcp://192.168.1.100:6565/TCMSConnectionService";  ip yi dinamik almak icin yukar�daki sat�rlar� yazd�k

        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
        ChannelFactory<ITCMSConnectionService> factory = new ChannelFactory<ITCMSConnectionService>(binding);
        var endpoint = new EndpointAddress(address);
        M3_Ybs.GlobalVariablesDTO.proxy = factory.CreateChannel(endpoint); //GlobalVariablesDTO s�n�f�nda tan�mlam�s oldugumuz degiskenle Mpu dan ald�g�m�z b�t�n verileri kendi Global degiskenlerimize esitledik

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();
        var address = configuration.GetSection("ServiceSettings")["ServiceAddress"];
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTimeOffset.Now.Hour == 12)
            {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(address);
                var response = await client.GetAsync(address);

                _logger.LogInformation("iSTENEN SAATTE �STEK YAPILDI at: {time}", DateTimeOffset.Now);
            }
            _logger.LogInformation("Worker �al���yooooo at: {time}", DateTimeOffset.Now);
            TimeSpan delay = TimeSpan.FromHours(1);
            await Task.Delay(delay, stoppingToken);

        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Durdurma i�lemleri buraya eklenebilir.
        // �rne�in, proxy ve factory nesnelerini kapatma i�lemleri.

        return Task.CompletedTask;
    }
}
