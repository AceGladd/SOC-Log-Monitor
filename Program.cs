using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static List<string> rules = new List<string>(); //kuralların tutulduğu liste
    static List<string> LogFilePaths = new List<string>(); // log dosyalarının yollarının tutulduğu liste

    static string RulesFile = "rules.yaml"; // kuralların listesinin bulunduğu dosya
    static string PathsFile = "paths.yaml"; // log dosyası yollarının tutulduğu dosya

    static void Main(string[] args)
    {

        Console.OutputEncoding = System.Text.Encoding.UTF8; // konsol çıktısı için UTF8 kodlaması - Türkçe karakterler için -

        string currentDirectory = Directory.GetCurrentDirectory(); // programın çalıştığı dizin

        if (currentDirectory.Contains("bin") && currentDirectory.Contains("Debug")) // eğer program derlenmiş bir dizinde çalışıyorsa, proje kök dizinine geçiş yapar
        {
            var parentDir = Directory.GetParent(currentDirectory)?.Parent?.Parent; // proje kök dizinine geçiş
            if (parentDir != null && parentDir.FullName != null)
            {
                currentDirectory = parentDir.FullName;
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("   ╔══════════════════════════════════════════════╗");
        Console.WriteLine("   ║           ALTAY SOC ANALİZ ARACI             ║");
        Console.WriteLine("   ╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        LoadRules(currentDirectory); // kuralları yükle
        LoadLogPaths(currentDirectory); // log dosyası yollarını yükle

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n--------------------------------------------------------");
            Console.WriteLine("              --- ALTAY SOC ANALİZ ARACI ---\n");
            Console.WriteLine(" [1] Statik Analiz Modu (Tek Seferlik)");
            Console.WriteLine(" [2] Dinamik İzleme Modu (Live Monitor)");
            Console.WriteLine(" [3] Yeni Log Dizini Ekleme");
            Console.WriteLine(" [4] İzlenen Log Dizini Listesini Göster");
            Console.WriteLine(" [5] Log Dizin Silme İşlemi");
            Console.WriteLine(" [0] Çıkış");
            Console.WriteLine("--------------------------------------------------------");
            Console.Write("Seçim: ");
            Console.ResetColor();

            string input = Console.ReadLine() ?? ""; // kullanıcıdan bir girdi alınır, boşsa boş string atanır

            if (int.TryParse(input, out int choice)) // girdinin bir sayı olup olmadığı kontrol edilir ve "choice" değişkenine atanır
            {
                if (choice < 6 && choice >= 0)
                {
                    switch (choice)
                    {
                        case 0:
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine("\n>>> Program kapatılıyor...");
                            Console.WriteLine(">>> Bizi tercih ettiğiniz için teşekkür ederiz :)");
                            Console.ResetColor();
                            Thread.Sleep(1000); // 1 saniye bekle
                            return;
                        case 1:
                            ScanListedFiles(currentDirectory); // Listedeki dosyaları bir kez tara
                            break;
                        case 2:
                            StartLiveMonitoring(currentDirectory); // Sürekli izlemeyi başlat
                            break;
                        case 3:
                            AddLogPath(currentDirectory); // Yeni log dizini ekle
                            break;
                        case 4:
                            ShowLogPaths(); // Mevcut listeyi görüntüle
                            break;
                        case 5:
                            RemoveLogPath(currentDirectory); // Log dizini sil
                            break;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n>>> Geçersiz sayı, lütfen 1 ile 6 arasında bir değer giriniz");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n>>> Geçersiz girdi");
                Console.ResetColor();
            }

        }
    }

    // Listedeki tüm dosyaları program kapatılana kadar sürekli tarayan fonksiyon
    static void StartLiveMonitoring(string currentDirectory)
    {
        if (!ValidateLogFiles()) return;

        Console.WriteLine("\n>>> Sürekli izleme aktif edildi - (Durdurmak için CTRL+C)");
        Dictionary<string, long> fileSizes = new Dictionary<string, long>(); // her dosyanın son bilinen boyutunu tutar, böylece yeni eklenen verileri tespit edilir
        // bu sadece eklenen yeni veriler taranır, dosya sürekli en baştan taranmaz

        while (true)
        {
            foreach (var path in LogFilePaths) // listedeki her dosya yolu için döngü
            {
                if (File.Exists(path)) // dosya mevcutsa if içine girer
                {
                    FileInfo file = new FileInfo(path); // dosya bilgilerini almak için FileInfo nesnesi

                    if (!fileSizes.ContainsKey(path)) // dosya daha önce izlenmemişse if içine girer
                    {
                        fileSizes[path] = file.Length; // dosyanın mevcut boyutunu kaydeder
                    }
                    else if (file.Length > fileSizes[path]) // dosya boyutu artmışsa if içine girer ve yeni eklenen verileri tarar
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("\n[Tespit] " + Path.GetFileName(path) + " güncellendi");
                        Console.ResetColor();

                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Seek(fileSizes[path], SeekOrigin.Begin);
                            using (var sr = new StreamReader(fs))
                            {
                                string? line;   // ? null olması durumunda hata vermemesi için
                                while ((line = sr.ReadLine()) != null)
                                {
                                    AnalyzeLogLine(line, currentDirectory, path);
                                }
                                // --- COZUM BURADA ---
                                // fs kapanmadan hemen once pozisyonu guncelliyoruz
                                fileSizes[path] = fs.Position;
                            }
                        }
                    }
                }
            }
            Thread.Sleep(3000); // 3 saniye bekle ve sonra tekrar kontrol et
        }
    }

    static bool ValidateLogFiles() // izlenecek dosyaların geçerliliğini kontrol eden fonksiyon
    {
        if (LogFilePaths.Count == 0) // izlenecek dosya yolu yoksa yani liste boşsa if içine girer ve bu uyarıyı verir
        {
            Console.WriteLine("İzlenecek dosya listesi boş.");
            return false;
        }

        bool anyFileExists = false;

        foreach (var path in LogFilePaths) // listedeki her dosya yolu için döngü, eğer en az bir dosya mevcutsa döngü kırılır
        {
            if (File.Exists(path))
            {
                anyFileExists = true;
                return true; // en az bir dosya mevcutsa true döner
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n>>> UYARI: {path} dosyası bulunamadı!");
                Console.ResetColor();
                anyFileExists = false; // en az bir dosya mevcut değilse false döner
            }
        }

        if (!anyFileExists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n>>> HATA: Listede kayıtlı olan dosyaların hiçbiri bulunamadı ya da uygun formata sahip değil");
            Console.ResetColor();
            return false; // Fonksiyondan çık, izlemeyi başlatma
        }


        return true;
    }

    static bool IsFileSupported(string filePath)
    {
        var allowedExtensions = new List<string> { ".txt", ".log", ".csv", ".json", ".xml" }; // Desteklenen dosya uzantıları

        // Dosya uzantısını al ve küçük harfe çevir
        string extension = Path.GetExtension(filePath).ToLower();

        return allowedExtensions.Contains(extension); // Uzantının izin verilenler arasında olup olmadığını kontrol eder
    }

    static void ScanListedFiles(string currentDirectory) // listedeki dosyaları bir kez tarayan fonksiyon
    {
        if (!ValidateLogFiles()) // izlenecek dosyaların uygunluğunu kontrol et, geçerli değilse fonksiyondan çık
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n>>> Tek seferlik tarama yapılıyor...");
        foreach (var path in LogFilePaths)
        {
            if (File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n--------------------------------------------------------");
                Console.WriteLine(">>> Analiz Edilen dosya: " + Path.GetFileName(path) + "\n");


                string[] fileLines = File.ReadAllLines(path); // dosyanın tüm satırlarını oku
                for (int i = 0; i < fileLines.Length; i++)
                {
                    string currentLine = fileLines[i]; // mevcut satırı al
                    AnalyzeLogLine(currentLine, currentDirectory, path); // satırı analiz edecek fonksiyona gönder
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--------------------------------------------------------");
                Console.ResetColor();
            }
        }

    }

    static void RemoveLogPath(string currentDirectory) // listedeki bir log dosyası yolunu silen fonksiyon
    {
        while (true)
        {

            if (LogFilePaths.Count == 0) // izlenecek dosya yolu yoksa yani liste boşsa if içine girer ve bu uyarıyı verir
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n>>> İzlenecek dosya listesi bos");
                Console.ResetColor();
                break;
            }

            ShowLogPaths(); // mevcut log dosyası yollarının listesini gösterir
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n>>> İşlemi iptal etmek için 0 giriniz");
            Console.ResetColor();

            Console.Write("\nSilinecek numara: ");

            String input = Console.ReadLine() ?? "";

            if (int.TryParse(input, out int number)) // girdinin bir sayı olup olmadığı kontrol edilir ve eğer sayıysa "number" değişkenine atanır
            {
                if (number > 0 && number <= LogFilePaths.Count) // geçerli bir sayı girildiyse if içine girer
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n>>> Dizin silindi: " + LogFilePaths[number - 1]);
                    Console.ResetColor();
                    LogFilePaths.RemoveAt(number - 1); // listeden belirtilen dizini siler
                    SaveLogPaths(currentDirectory); // güncellenmiş listeyi dosyaya kaydeder
                    break;
                }
                else if (number == 0) // kullanıcı işlemi iptal etmek istediğinde burası aktif olur
                {
                    Console.WriteLine("\n>>> Dizin silme işlemi iptal edildi");
                    break;
                }
                else // geçersiz sayı girildiğinde burası aktif olur
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n>>> Geçersiz sayı, lütfen 1 ile 6 arasında bir değer giriniz");
                    Console.ResetColor();
                }
            }
            else // geçersiz girdi girildiğinde burası aktif olur
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n>>> Geçersiz girdi");
                Console.ResetColor();
            }
        }
    }

    static void SaveLogPaths(string currentDirectory) // log dosyası yollarını paths.yaml dosyasına kaydeden fonksiyon
    {
        string fullPath = Path.Combine(currentDirectory, PathsFile); // paths.yaml dosyasının tam yolu

        File.WriteAllLines(fullPath, LogFilePaths); // Listeyi olduğu gibi satır satır dosyaya yazar
    }

    static void LoadLogPaths(string currentDirectory)
    {
        string fullPath = Path.Combine(currentDirectory, PathsFile);

        if (File.Exists(fullPath)) // dosya mevcutsa if içine girer
        {
            string[] lines = File.ReadAllLines(fullPath); // dosyanın tüm satırlarını okur ve bir diziye atar
            LogFilePaths = new List<string>(lines); // diziyi listeye dönüştürür
        }
    }

    static void LoadRules(string currentDirectory) // kuralları rules.yaml dosyasından yükleyen fonksiyon
    {
        string fullPath = Path.Combine(currentDirectory, RulesFile); // rules.yaml dosyasının tam yolu

        if (File.Exists(fullPath)) // dosya mevcutsa if içine girer
        {
            string[] lines = File.ReadAllLines(fullPath); // dosyanın tüm satırlarını okur ve bir diziye atar

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line)) // boş olmayan satırları kurallar listesine ekler
                {
                    rules.Add(line.Trim()); // satırdaki boşlukları temizler ve listeye ekler
                }
            }
        }
    }

    static void AddLogPath(string currentDirectory) // yeni bir log dosyası yolu ekleyen fonksiyon
    {
        Console.Write("\nTam Yol: ");
        string path = Console.ReadLine() ?? ""; // kullanıcıdan yeni log dosyası yolu alınır
        if (!string.IsNullOrEmpty(path)) // boş olmayan bir girdi alındıysa if içine girer
        {
            if (File.Exists(path))
            { // dosya mevcutsa if içine girer
                if (IsFileSupported(path)) // dosya uzantısı destekleniyorsa if içine girer
                {
                    LogFilePaths.Add(path.Trim());
                    SaveLogPaths(currentDirectory); // güncellenmiş listeyi dosyaya kaydeder
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\n>>> Dizin eklendi: " + path);
                    Console.ResetColor();
                }
                else // dosya uzantısı desteklenmiyorsa else içine girer
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n>>> HATA: Desteklenmeyen dosya formatı! Sadece .txt, .log, .csv, .json, .xml uzantılı dosyalar desteklenmektedir.");
                    Console.ResetColor();
                }
            }
            else // dosya bulunamadıysa else içine girer
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n>>> HATA: Belirtilen dosya bulunamadı!");
                Console.ResetColor();
            }
        }
        else // kullanıcıdan boş bir girdi alındıysa else içine girer
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n>>> Herhangi bir girdi alınamadı");
            Console.ResetColor();
        }
    }

    static void ShowLogPaths() // mevcut log dosyası yollarını listeleyen fonksiyon
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--------------------------------------------------------");
        Console.WriteLine("              --- Log Dosya Dizinleri ---\n");

        for (int i = 0; i < LogFilePaths.Count; i++) // listedeki her log dosyası yolu için for döngüsü
        {
            Console.WriteLine((i + 1) + " - " + LogFilePaths[i]);
        }

        Console.WriteLine("--------------------------------------------------------");
        Console.ResetColor();
    }


    static void AnalyzeLogLine(string log, string currentDirectory, string sourceFilePath) // bir log satırını kurallara göre analiz eden fonksiyon
    {
        string logMessagePart = log; // log satırının analiz edilecek kısmı

        int splitIndex = log.IndexOf(": "); // log satırında ": " karakterlerinin indexini bulur ve bu karakterlerden sonrasını analiz eder

        if (splitIndex > -1) // eğer ": " karakterleri bulunursa if içine girer
        {
            logMessagePart = log.Substring(splitIndex + 2); // log satırını ": " karakterlerinden sonrasını alır
        }

        foreach (var rule in rules) // kurallar listesinde döngü oluşturur
        {
            if (logMessagePart.ToLower().Contains(rule.ToLower())) // eğer log satırında kural geçiyorsa, yani eşleşme varsa if içine girer
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ALERT] {rule.ToUpper()} -> {log}"); // konsola uyarı mesajı yazdırır
                Console.ResetColor();

                LogToCsv(rule, log, sourceFilePath, currentDirectory); // uyarıyı rapor dosyasına kaydeder
            }
        }
    }

    static void LogToCsv(string rule, string logLine, string sourceFilePath, string currentDirectory)
    {
        string reportsDirectory = Path.Combine(currentDirectory, "Raporlar"); // raporların kaydedileceği klasör yolu

        if (!Directory.Exists(reportsDirectory)) // rapor klasörü yoksa oluşturur
        {
            Directory.CreateDirectory(reportsDirectory); // klasörü oluşturur
        }

        string fileName = Path.GetFileName(sourceFilePath); // log dosyasının adını alır
        string reportFileName = $"rapor_{fileName}.csv"; // rapor dosyasının adı

        string fullFilePath = Path.Combine(reportsDirectory, reportFileName); // rapor dosyasının tam yolu
        string cleanLog = logLine.Replace(",", " "); // log satırını temizle - csv formatı bozulmaması için -

        string checkData = $"{rule},{cleanLog}"; // kontrol edilecek veri: kural ve log satırı

        if (File.Exists(fullFilePath)) // rapor dosyası mevcutsa içini kontrol et
        {
            string currentContent = File.ReadAllText(fullFilePath); // dosyanın tüm içeriğini oku
            if (currentContent.Contains(checkData)) // eğer bu kayıt dosyada zaten varsa
            {
                return; // tekrar yazma ve fonksiyondan çık
            }
        }

        string reportData = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{checkData}"; // rapor satırı formatı: zaman, kural, log satırı

        File.AppendAllText(fullFilePath, reportData + Environment.NewLine); // rapor satırını dosyaya ekler
    }
}