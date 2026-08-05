# Packaging Placeholders

Bu klasor platform bazli paketleme dosya iskeletini tutar.

## Dosyalar

- `app.manifest`: Windows publish/installer adimi icin placeholder.
- `Info.plist.template`: macOS app bundle metadata template placeholder.
- `devcrew.desktop.template`: Linux desktop entry template placeholder.

## Publish Plani

- Runtime hedefleri: `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`
- macOS release akisi paketleme icin ayrintili bir workflow ile baglidir.
- Windows release akisi MSIX installer uretir.
- Bu klasordeki dosyalar platform bazli packaging kararlarini destekleyen iskeletler olarak tutulur.

## VS Code Debug Notu

- macOS uzerinde `dotnet build` sonrasinda `bin/Debug/net10.0/DevCrew.Desktop.app` olusturulur.
- Dock ve `Command+Tab` icon davranisini dogrulamak icin VS Code icinden bundle executable debug etmek daha dogru sonuc verir.
- Guncelik gelistirme sirasinda istenirse proje tabanli debug korunabilir; bundle debug daha cok macOS davranisini dogrulama amacli kullanilmalidir.

## Not

Bu dosyalar minimal iskelet olarak eklendi. Release asamasinda platform gereksinimlerine gore guncellenebilir.
