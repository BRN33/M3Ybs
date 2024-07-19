# 👏M3-Ybs - Yolcu Bilgilendirme Sistemi👏

✔ TCMS den alınan tren verileri işlenerek , Makinist ekranından kurulan rota bilgileri kullanılarak Yolcu Bilgilendirme ekranlarında istasyon bilgilerini gösteren bir projedir.
✔ WCF servisi ile MPU ya baglanıp tren üzerinde tako, hız ve kapı durumu gibi anlık verileri almaktadır.
✔ Makinist ekranına Rest Api ile istek atarak  ekrandan kurulan rotayı alıp stationData.json dosyasına kaydetmektedir.
✔ Trenden aldığı tako verisini baz alarak istasyonlar arası mesafeyi hesaplamaktadır.
✔ Şuan ki istasyon, Gelecek istasyon ve Hedef istasyon gibi bilgileri Yolcu Bilgilendirme Ekranlarına Rest Api üzerinden göndermektedir.
✔ İstasyon anonslarını WCF servisi üzerinden MPU ya göndererek yapmaktadır.
✔ İstasyon arası mesafe ve toplam mesafeyi Rest Api üzerinden Makinist ekranına göndermektedir.
✔ Eğer bir istasyon pass geçilip gitmek istenirse, pass geçilecek istasyon mesafeleri bir sonraki istasyon mesafesine eklenerek algoritma ona göre devam etmektedir.



💢Bu yazılımı x64 bilgisayarlarda çalıştırmak için;💢
proje dizininde cmd komut satırını açıp 
"dotnet publish -c Release -r win-x64 --output ./MyTargetFolder MySolution.sln"    komutunu çalıştırmak yeterlidir.
Bu bize  başka bilgisayarlarda çalıştırabilir bir dosya oluşturur.💢

💢Bu yazılımı x86 bilgisayarlarda çalıştırmak için;
proje dizininde cmd komut satırını açıp 
"dotnet publish -c Release -r win-x86 --output ./MyTargetFolder MySolution.sln"    komutunu çalıştırmak yeterlidir.
Bu bize  başka bilgisayarlarda çalıştırabilir bir dosya oluşturur.💢



🎬PROJEYİ ÇALIŞTIRMAK İÇİN;🎬
Publish klasörünün altında bulunan exe dosyasını çalıştırmak yeterlidir.
exe kısayolunu oluşturup "Başlangıçta Açılan Uygulamalar" klasörüne eklendiğinde bilgisayar her açıldığında otomatik olarak açılacaktır. 


💻Ip Yapılandırması için;
appsettings.josn   dosyası açılıp içerinden gerekli alanlar değiştirilmelidir.🖥

  "ServiceSettings": {
    "mpu_Service_Endpoint": "net.tcp://10.3.156.130:8733/M3YBSCommunication/M3YBSCommunication",//MPU Servis Ip Adresi

    "ipAddress": "https://10.3.156.120:7042", //Yönetim Servis Ip Adresi
    "apiManager_endpoint": "https://10.3.156.120:7042/api/Json/GetData", //Yönetim Servis Ip Adresi

    "ddu_ekran_ip": "https://10.3.156.130:7069/api/Home/GetRoute", //DDU Ekran  Servis Ip Adresi

    "istasyondanCıkısMesafesi": "20", //İstasyondan cıktıktan sonraki anons yapma mesafesi
    "istasyonaKalanMesafe": "200" //İstasyona kalan mesafeye göre anons yapma mesafesi

  }


 MPU servisine baglantı sağlamak için 🖥
 - ConnectedService.json  dosyasından    "net.tcp://10.3.156.130:8733/M3YBSCommunication/M3YBSCommunication"     kısmındaki sadece ip adresi değiştirilmelidir. Port numarası aynı kalmalıdır.💻

 

 ### Kullanılan Teknolojiler:  
 - .Net Core 5.0  
 - WCF 
 - Rest Api
 - BackgroundServices