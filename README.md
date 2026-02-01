# ALTAY SOC Analiz Aracı 🛡️

Bu proje, C# tabanlı geliştirilmiş basit bir log analiz ve tehdit tespit yazılımıdır. Sistem, sunucu loglarındaki şüpheli aktiviteleri kolayca tespit ederek uyarı şeklinde bildirir. Yazılım, log dosyalarını belirlenen kurallara göre tarar ve sonuçları raporlar.


## ⚙️ Temel Yetenekler
Yazılım, log analizi için iki çalışma moduna ve yönetim modlarına sahiptir:

* **Manuel Tarama:** Listeye eklenmiş dosyaları tek seferlik tarar ve tespit edilen tehditler için tarama sonunda, log dosyasının ismi kullanılarak özel bir rapor oluşturulur.
* **Dinamik İzleme (Live Monitor):** Dosyaları arka planda sürekli takip eder. Log dosyasına yeni bir veri düştüğünde yakalar ve alarm verir. Bu tarama işlemi her 3 saniyede bir gerçekleşir. Tespit edilen uyarılar rapora anlık olarak işlenir. Bu işlem program kapatılana kadar devam eder.
* **Log Dizini Ekleme:** Analiz edilmek istenen yeni log dosyalarının tam dosya yolu girilerek listeye eklenir. Eğer girilen dizinde dosya mevcut değilse veya dosya formatı uygun değilse bu işlem hata verir ve gerçekleşmez. Erişim sorunu olması durumunda da işlem gerçekleşmez.
* **Listeden Log Dizini Silme:** Artık takip edilmesi gerekmeyen veya hatalı eklenen dosyaların tarama listesinden çıkarılmasına olanak tanır.
* **Mevcut Listeyi Görüntüleme:** Sisteme kayıtlı olan ve o an aktif olarak izlenen tüm log dosyalarının listesini ekrana getirir.


## ⚙️ Teknik Detaylar
Programın dosya okuma aracı, sistem kaynaklarını için optimize edilmiştir:

* **Dosya Boyut Takibi:** Dosyalar her seferinde en baştan okunmaz. Program nerede kaldığını hafızada tutar ve sadece dosyaya yeni eklenen satırları analiz eder. Bu kısım sadece *dinamik analiz* modu için geçerlidir. Manuel taramada dosya her zaman en baştan taranır ancak raporda zaten yazılan uyarıları tekrar yazmaz.
* **Veri Formatları:** `.txt`, `.log`, `.csv`, `.json` ve `.xml` uzantılı dosyalar desteklenmektedir. Diğer dosyalar tarama işlemi için listeye eklenemez.
* **Çift Kayıt Kontrolü:** Aynı hata logu daha önce raporlanmışsa, rapor dosyasını bozmamak için tekrar kayıt yapılmaz. Bu kısım manuel tarama için ek kontrol amacıyla eklenmiştir.


## 📂 Kurulum ve Yapılandırma
Yazılımı kullanmak için bir kuruluma gerek yoktur. Ancak aracı çalıştırmak için kuralların belirlenmesi gerekir.

### 1. Kuralları Düzenleme (rules.yaml)
Program tehditleri bu dosyadaki değerlere göre algılar. Proje ana dizinindeki rules.yaml dosyasını metin düzenleyici ile açıp alt alta kural ekleyebilirsiniz.

**Örnek `rules.yaml` içeriği:**

#kurallar listesi
rules:
  - failed password
  - error
  - ssh
  - denied

### 2. Log Dosyası Yolu Ekleme (paths.yaml)
Programı çalıştırdığınız zaman menüden “3” tuşuna basarak log dosyasına ait izlenecek tam yolunu girebilirsiniz. Program bu yolları otomatik olarak paths.yaml dosyasına kaydeder.


## 🐳 Docker ile Çalıştırma
Projeyi Docker konteyneri içinde izole bir şekilde çalıştırabilirsiniz.

### 1. İmajı Oluşturun: Terminali proje klasöründe açın ve şu komutu girin:
docker build -t altay-soc .

### 2. Konteyneri Başlatın: Bulunduğunuz dizindeki logların okunabilmesi ve dosyalara erişilebilmesi için aşağıdak sisteminize uygun olan komutu kullanın:

* **Windows:**

docker run -it --rm -v ${PWD}:/app altay-soc


* **Linux:**

docker run -it --rm -v $(pwd):/app altay-soc


## 📊 Raporlama Sistemi
Program bir tehdit tespit ettiğinde CSV formatında rapor üretir.

Konum: Rapor dosyaları, ana proje klasörünüze tarama sonrasında otomatik olarak kaydedilir.

Dosya Adı: Her kaynak log dosyası için ayrı rapor oluşur.

Format: Tarih, Kural, Log İçeriği şeklindedir.
