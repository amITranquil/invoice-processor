# Fatura İşleme Sistemi - Kullanım Kılavuzu

## 🚀 Sistem Başlatma

### 1. Otomatik Başlatma (Windows)
```cmd
build_windows.bat
cd dist
start_invoice_processor.bat
```

### 2. Manuel Geliştirme Ortamı
```bash
# Terminal 1 - Backend
cd InvoiceProcessor.Api/InvoiceProcessor.Api
dotnet run

# Terminal 2 - Frontend
cd invoice_processor
flutter run -d windows
```

## 📄 Fatura Yükleme ve İşleme

### Desteklenen Formatlar
- **PDF**: Text-based ve image-based PDF dosyaları
- **Resim**: JPG, JPEG, PNG, TIFF formatları
- **Dil**: Türkçe ve İngilizce OCR desteği

### Fatura Türleri
1. **Alış Faturası**: Satın alınan ürünler (stok artar)
2. **Satış Faturası**: Satılan ürünler (stok azalır)
3. **Alış İadesi**: İade edilen alışlar (stok azalır)
4. **Satış İadesi**: İade edilen satışlar (stok artar)

### Upload Süreci
1. **Fatura Türü Seçimi**: Alış veya Satış faturası seçin
2. **Dosya Seçimi**: "Dosya Seç" butonuna tıklayın
3. **Upload**: "Yükle ve İşle" butonuna tıklayın
4. **İşleme**: Sistem otomatik olarak:
   - OCR/PDF text extraction
   - Fatura bilgilerini parse eder
   - Ürünleri tanımlar
   - Güven skoru hesaplar

## 🔍 Fatura Parsing Özellikleri

### Otomatik Çıkarılan Bilgiler
- **Fatura Numarası**: "Fatura No", "Invoice No", "Belge No"
- **Tarih**: DD/MM/YYYY formatında
- **Firma Bilgileri**: Tedarikçi/Müşteri isimleri
- **Toplam Tutar**: "Toplam", "Total", "Genel Toplam"
- **KDV**: "KDV", "VAT" tutarları

### Ürün Tanıma Patterns
```
Pattern 1: Ürün Adı | Miktar | Birim | Birim Fiyat | Toplam
Pattern 2: Ürün Kodu | Ürün Adı | Miktar | Birim Fiyat | Toplam  
Pattern 3: Ürün Adı | Toplam Fiyat
Pattern 4: Tab separated values
```

### Güven Skoru Hesaplaması
- **Base Score**: 50 puan
- **Fatura Numarası**: +15 puan
- **Tarih**: +15 puan
- **Firma Bilgisi**: +10 puan
- **Toplam Tutar**: +10 puan
- **Ürünler**: +20 puan
- **Text Kalitesi**: +10 puan

## 📦 Stok Yönetimi

### Otomatik Stok Güncellemesi
Faturalar onaylandığında stok otomatik güncellenir:

| Fatura Türü | Stok Etkisi |
|-------------|-------------|
| Alış | Stok Artar (+) |
| Satış | Stok Azalır (-) |
| Alış İadesi | Stok Azalır (-) |
| Satış İadesi | Stok Artar (+) |

### Ürün Eşleştirme
1. **Ürün Kodu**: Önce ürün koduna göre arama
2. **Ürün Adı**: Fuzzy matching ile isim eşleştirme
3. **Yeni Ürün**: Bulunamazsa otomatik ürün oluşturma

### Stok Takip
- **Mevcut Stok**: Anlık stok seviyeleri
- **Minimum Stok**: Düşük stok uyarıları
- **Hareket Geçmişi**: Tüm stok hareketleri
- **Son Alış Fiyatı**: Fiyat takibi

## 📊 Dashboard ve Raporlar

### Ana İstatistikler
- Toplam fatura sayısı
- Bekleyen onaylar
- Günlük işlemler
- Toplam ciro

### Stok Raporları
- Toplam ürün sayısı
- Düşük stok uyarıları
- Toplam stok değeri
- Hareket analizi

## 🔧 Sistem Ayarları

### Database
- **SQLite**: Local veritabanı
- **Auto Migration**: İlk çalıştırmada otomatik tablo oluşturma
- **Backup**: Manuel yedekleme önerilir

### OCR Konfigürasyonu
```
backend/tessdata/
├── tur.traineddata  # Türkçe dil paketi
└── eng.traineddata  # İngilizce dil paketi
```

### API Endpoints
- **Upload**: `POST /api/invoices/upload`
- **List**: `GET /api/invoices`
- **Approve**: `POST /api/invoices/{id}/approve`
- **Products**: `GET /api/products`
- **Stock**: `GET /api/stock/movements`

## 🐛 Sorun Giderme

### Upload Sorunları
1. **Dosya formatı**: Desteklenen formatları kontrol edin
2. **Dosya boyutu**: Çok büyük dosyalar sorun yaratabilir
3. **OCR kalitesi**: Net, düz, yüksek çözünürlüklü dosyalar kullanın

### Parsing Sorunları
1. **Düşük güven skoru**: Manuel düzenleme gerekebilir
2. **Eksik ürünler**: Fatura formatı tanınmıyor olabilir
3. **Yanlış tutarlar**: Decimal separator (,/.) sorunları

### Backend Sorunları
1. **Port 5002 meşgul**: `netstat -ano | findstr :5002`
2. **Database lock**: Uygulamayı yeniden başlatın
3. **OCR hatası**: Tesseract dil dosyalarını kontrol edin

## 📝 Test Senaryoları

### Örnek Test Faturaları
1. **test_invoice.txt**: Satış faturası örneği
2. **test_purchase_invoice.txt**: Alış faturası örneği

### Test Adımları
1. Fatura türünü seçin
2. Test dosyasını upload edin
3. Parse sonuçlarını kontrol edin
4. Güven skorunu değerlendirin
5. Faturayı onaylayın
6. Stok değişikliklerini kontrol edin

### Beklenen Sonuçlar
- **Güven Skoru**: 70+ olmalı
- **Ürün Tanıma**: Ana ürünler tanınmalı
- **Tutar Eşleşmesi**: Toplam tutar doğru olmalı
- **Stok Güncellemesi**: Onay sonrası stok değişmeli

## 🔐 Güvenlik Notları

- Hassas fatura verilerini güvenli ortamda saklayın
- Veritabanını düzenli yedekleyin
- Dosya upload sınırlarını göz önünde bulundurun
- Test verilerini production'da kullanmayın

---

💡 **İpucu**: En iyi sonuçlar için, düz, net ve yüksek çözünürlüklü fatura görüntüleri kullanın!