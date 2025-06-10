# Invoice Processor - Windows Deployment Guide

## Windows için Derleme ve Dağıtım Rehberi

### Gereksinimler
- Windows 10/11
- .NET 8.0 SDK
- Flutter SDK
- Visual Studio Code veya Visual Studio

### Otomatik Derleme

1. **Projeyi Windows makinesine kopyalayın**
2. **build_windows.bat dosyasını yönetici olarak çalıştırın:**
   ```cmd
   build_windows.bat
   ```

Bu script şunları yapar:
- C# Backend API'yi tek dosya olarak derler (self-contained)
- Flutter uygulamasını Windows için derler
- Her ikisini `dist/` klasöründe birleştirir
- Otomatik başlatma scripti oluşturur

### Manuel Derleme

#### 1. Backend (C# API) Derleme
```cmd
cd InvoiceProcessor.Api\InvoiceProcessor.Api
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\backend
```

#### 2. Frontend (Flutter) Derleme
```cmd
cd invoice_processor
flutter build windows --release
```

### Dağıtım Yapısı

Derleme sonrası `dist/` klasöründe şu yapı oluşur:

```
dist/
├── backend/
│   └── InvoiceProcessor.Api.exe
├── app/
│   ├── invoice_processor.exe
│   ├── flutter_windows.dll
│   └── data/
└── start_invoice_processor.bat
```

### Çalıştırma

1. **Otomatik Başlatma:**
   - `start_invoice_processor.bat` dosyasını çift tıklayın
   - Bu hem backend'i hem de frontend'i başlatır

2. **Manuel Başlatma:**
   ```cmd
   # Terminal 1 - Backend
   cd dist\backend
   InvoiceProcessor.Api.exe
   
   # Terminal 2 - Frontend
   cd dist\app
   invoice_processor.exe
   ```

### Özellikler

#### Otomatik Backend Yönetimi
- Flutter uygulaması açılırken backend otomatik başlar
- Uygulama kapanırken backend otomatik durur
- Backend bulunamazsa hata mesajı gösterir

#### Veritabanı
- SQLite kullanır (InvoiceProcessor.db)
- İlk çalıştırmada otomatik oluşturulur
- Backend ile aynı klasörde saklanır

#### Loglar
- Backend logları: `backend/logs/` klasöründe
- Konsol çıktısı ve dosya logları

### Sorun Giderme

#### Backend Başlatma Sorunları
1. **Port 5002 meşgul:** Başka uygulama kullanıyor olabilir
2. **Executable bulunamadı:** Dosya yollarını kontrol edin
3. **Veritabanı hatası:** Yazma izinlerini kontrol edin

#### Windows Defender Uyarıları
1. "Windows korudu" uyarısında:
   - "Daha fazla bilgi"ye tıklayın
   - "Yine de çalıştır" seçin
2. Veya dosyayı güvenlik istisnalarına ekleyin

### Geliştirme Ortamında Test

```cmd
# Backend başlat
cd InvoiceProcessor.Api\InvoiceProcessor.Api
dotnet run

# Flutter başlat (ayrı terminal)
cd invoice_processor
flutter run -d windows
```

### Port Yapılandırması

- Backend API: http://localhost:5002
- HTTPS (geliştirme): https://localhost:5001
- Flutter otomatik olarak HTTP portunu kullanır

### ⚠️ Önemli Tesseract OCR Ayarları

Backend'in OCR işlevselliği için Tesseract dil dosyalarına ihtiyacı vardır:

1. **Tesseract dil dosyalarını indirin:**
   - Türkçe: https://github.com/tesseract-ocr/tessdata/raw/main/tur.traineddata
   - İngilizce: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata

2. **Dil dosyalarını backend klasörüne kopyalayın:**
   ```
   dist/backend/tessdata/
   ├── tur.traineddata
   └── eng.traineddata
   ```

3. **Alternatif olarak Tesseract'ı sistem genelinde kurun:**
   - Windows: https://github.com/UB-Mannheim/tesseract/wiki
   - Kurulum sonrası tessdata klasörü otomatik bulunur

### Ek Notlar

- Tesseract OCR kütüphanesi backend'e dahildir
- iText7 PDF işleme kütüphanesi dahildir
- Tüm bağımlılıklar self-contained derlemede bulunur
- İnternet bağlantısı gerekmez (local çalışır)
- İlk çalıştırmada SQLite veritabanı otomatik oluşturulur