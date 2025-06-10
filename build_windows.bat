@echo off
echo Building Invoice Processor for Windows...

echo.
echo Step 1: Building C# Backend API...
cd "InvoiceProcessor.Api\InvoiceProcessor.Api"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "..\..\dist\backend"
if %ERRORLEVEL% NEQ 0 (
    echo Failed to build backend!
    pause
    exit /b 1
)

echo.
echo Step 2: Building Flutter Frontend...
cd "..\..\invoice_processor"
flutter build windows --release
if %ERRORLEVEL% NEQ 0 (
    echo Failed to build frontend!
    pause
    exit /b 1
)

echo.
echo Step 3: Copying Flutter build to distribution folder...
if not exist "..\dist\app" mkdir "..\dist\app"
xcopy /E /I "build\windows\x64\runner\Release\*" "..\dist\app\"

echo.
echo Step 4: Downloading Tesseract language files...
if not exist "..\dist\backend\tessdata" mkdir "..\dist\backend\tessdata"
powershell -Command "Invoke-WebRequest -Uri 'https://github.com/tesseract-ocr/tessdata/raw/main/tur.traineddata' -OutFile '..\dist\backend\tessdata\tur.traineddata'" 2>nul || echo Warning: Could not download Turkish language file
powershell -Command "Invoke-WebRequest -Uri 'https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata' -OutFile '..\dist\backend\tessdata\eng.traineddata'" 2>nul || echo Warning: Could not download English language file

echo.
echo Step 5: Creating startup script...
cd "..\dist"
echo @echo off > start_invoice_processor.bat
echo cd /d "%%~dp0" >> start_invoice_processor.bat
echo start /B backend\InvoiceProcessor.Api.exe >> start_invoice_processor.bat
echo timeout /t 3 /nobreak ^>nul >> start_invoice_processor.bat
echo start app\invoice_processor.exe >> start_invoice_processor.bat

echo.
echo Step 6: Creating README...
echo Invoice Processor - Fatura İşleme ve Stok Yönetimi > README.txt
echo. >> README.txt
echo Kurulum: >> README.txt
echo 1. Bu klasörü istediğiniz yere kopyalayın >> README.txt
echo 2. start_invoice_processor.bat dosyasını çalıştırın >> README.txt
echo. >> README.txt
echo OCR İşlevselliği için: >> README.txt
echo - Tesseract dil dosyaları backend/tessdata/ klasöründe olmalı >> README.txt
echo - İnternet bağlantısı varsa otomatik indirilir >> README.txt
echo. >> README.txt
echo Not: İlk çalıştırmada Windows Defender uyarısı çıkabilir. >> README.txt
echo "Daha fazla bilgi" ve "Yine de çalıştır" seçeneklerini kullanın. >> README.txt

echo.
echo Build completed successfully!
echo.
echo Distribution files are in: %CD%
echo To run: Execute start_invoice_processor.bat
echo.
pause