# Invoice Processor - Fatura İşleme ve Stok Yönetimi Sistemi

🧾 **Modern fatura işleme ve stok takip sistemi** - OCR teknolojisi ile PDF/resim formatındaki faturaları otomatik okuyup stok hareketlerini yöneten masaüstü uygulaması.

## 🚀 Özellikler

### 📄 Fatura İşleme
- **OCR Desteği**: Tesseract ile Türkçe/İngilizce fatura okuma
- **PDF İşleme**: Text-based ve image-based PDF desteği
- **Resim Formatları**: JPG, PNG, TIFF desteği
- **Otomatik Parsing**: Fatura bilgilerini otomatik çıkarma
- **Onay Sistemi**: Manuel inceleme ve onay süreci

### 📦 Stok Yönetimi
- **Otomatik Stok Güncellemesi**: Onaylanan faturalardan otomatik stok hareketi
- **Ürün Tanıma**: Akıllı ürün eşleştirme (kod/isim bazlı)
- **Düşük Stok Uyarıları**: Minimum stok seviyesi takibi
- **Hareket Geçmişi**: Detaylı stok hareket kayıtları
- **İstatistikler**: Kapsamlı dashboard ve raporlar

### 💻 Teknik Özellikler
- **Masaüstü Uygulaması**: Windows, macOS, Linux desteği
- **Local Çalışma**: İnternet bağlantısı gerektirmez
- **Otomatik Backend**: Frontend açılışında backend otomatik başlar
- **Modern UI**: Flutter ile responsive tasarım
- **REST API**: Swagger dokümantasyonu ile

## 🏗️ Proje Yapısı

```
InvoiceStock/
├── InvoiceProcessor.Api/          # C# Backend API
│   ├── Controllers/               # API Controllers
│   ├── Services/                  # Business Logic
│   ├── Data/                      # Entity Framework
│   └── Models.cs                  # Data Models
├── invoice_processor/             # Flutter Frontend
│   ├── lib/
│   │   ├── models/               # Data Models
│   │   ├── providers/            # State Management
│   │   ├── services/             # API & Backend Service
│   │   ├── screens/              # UI Screens
│   │   └── widgets/              # Reusable Components
├── build_windows.bat             # Windows Build Script
└── DEPLOYMENT_GUIDE.md           # Deployment Instructions
```

## 🛠️ Teknoloji Stack

### Backend (C# .NET 8)
- **Framework**: ASP.NET Core Web API
- **Database**: SQLite (Entity Framework Core)
- **OCR**: Tesseract.NET
- **PDF**: iText7
- **Logging**: Serilog
- **Documentation**: Swagger/OpenAPI

### Frontend (Flutter)
- **Framework**: Flutter 3.6+
- **State Management**: Provider Pattern
- **HTTP Client**: Dio
- **File Handling**: File Picker
- **UI**: Material Design 3

## 🚀 Hızlı Başlangıç

### Windows için Otomatik Kurulum

1. **Repository'yi klonlayın:**
   ```bash
   git clone <repository-url>
   cd InvoiceStock
   ```

2. **Otomatik build çalıştırın:**
   ```cmd
   build_windows.bat
   ```

3. **Uygulamayı başlatın:**
   ```cmd
   cd dist
   start_invoice_processor.bat
   ```

### Geliştirme Ortamı

#### Gereksinimler
- .NET 8.0 SDK
- Flutter SDK 3.6+
- Visual Studio Code veya Visual Studio

#### Backend Çalıştırma
```bash
cd InvoiceProcessor.Api/InvoiceProcessor.Api
dotnet run
```

#### Frontend Çalıştırma
```bash
cd invoice_processor
flutter run -d windows
```

## 📖 API Dokümantasyonu

Backend çalışırken Swagger UI'a erişin:
- **Local**: http://localhost:5002/swagger
- **HTTPS**: https://localhost:5001/swagger

### Ana Endpoints
- `GET /api/invoices` - Fatura listesi
- `POST /api/invoices/upload` - Fatura yükleme
- `POST /api/invoices/{id}/approve` - Fatura onaylama
- `GET /api/products` - Ürün listesi
- `GET /api/stock/movements` - Stok hareketleri
- `GET /api/dashboard/stats` - Dashboard istatistikleri

## 🔧 Konfigürasyon

### OCR Ayarları
Tesseract dil dosyaları gereklidir:
```
backend/tessdata/
├── tur.traineddata    # Türkçe
└── eng.traineddata    # İngilizce
```

### Database
SQLite veritabanı otomatik oluşturulur:
- **Lokasyon**: `backend/InvoiceProcessor.db`
- **Migration**: İlk çalıştırmada otomatik

### Portlar
- **HTTP API**: 5002
- **HTTPS API**: 5001
- **Flutter**: Dinamik port

## 🐛 Sorun Giderme

### Yaygın Sorunlar

**1. Backend Başlatma Hatası**
```bash
# Port kontrol
netstat -ano | findstr :5002
# Process sonlandırma
taskkill /PID <pid> /F
```

**2. OCR Hatası**
- Tesseract dil dosyalarını kontrol edin
- İnternet bağlantısı ile otomatik indirme

**3. Flutter Build Hatası**
```bash
flutter clean
flutter pub get
flutter run
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/yeni-özellik`)
3. Değişikliklerinizi commit edin (`git commit -am 'Yeni özellik eklendi'`)
4. Branch'inizi push edin (`git push origin feature/yeni-özellik`)
5. Pull Request oluşturun

## 📝 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın.

## 📞 İletişim

- **Geliştirici**: Şakın Burak Çivelek
- **Email**: [email]
- **GitHub**: [github-profile]

## 🎯 Gelecek Özellikler

- [ ] Bulut senkronizasyonu
- [ ] Mobil uygulama desteği
- [ ] Çoklu dil desteği
- [ ] Gelişmiş raporlama
- [ ] REST API genişletme
- [ ] Docker containerization

---

⭐ **Bu projeyi beğendiyseniz star vermeyi unutmayın!**