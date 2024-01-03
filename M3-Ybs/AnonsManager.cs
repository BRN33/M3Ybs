using System.Diagnostics;
using System.IO;
using System.Media;

using M3_Ybs;
using Windows.Media.Playback;

public class AnonsManager
{
    private string AnonsRootPath { get; set; }

    public AnonsManager(string anonsRootPath)
    {
        AnonsRootPath = anonsRootPath;
    }

    public void PlayAnons(string stationName)
    {
        // İstasyonun Anons klasörünün yolu
        string stationAnonsPath = Path.Combine(AnonsRootPath, stationName);

        // İstasyonun Anons dosyasının tam yolu
        string anonsFilePath = Path.Combine(stationAnonsPath);

        // Eğer dosya varsa çal
        if (File.Exists(anonsFilePath))
        {
            // Dosyayı çalacak kod buraya gelecek (örneğin, bir ses kütüphanesi kullanılabilir)
            Debug.WriteLine($"Anons yapılıyor: {anonsFilePath}");
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.SoundLocation = anonsFilePath;
            soundPlayer.Play();

            //MediaPlayer mediaPlayer = new MediaPlayer();
            //mediaPlayer.Open(new Uri(anonsFilePath));
            //mediaPlayer.Play();


        }
        else
        {
            Debug.WriteLine($"Anons dosyası bulunamadı: {anonsFilePath}");
        }
    }
}
