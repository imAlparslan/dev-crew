# DevCrew Icon Hazirlama Rehberi

Bu dokuman, Windows, macOS ve Linux icin uygulama icon dosyalarini dogru formatta ve olcekte hazirlamak icin repo icindeki sabit yol sozlesmesini aciklar.

## 1. Platform Matrisi

| Platform | Gerekli Format | Sabit Dosya | Minimum Set |
| --- | --- | --- | --- |
| Windows | `.ico` | `Assets/icons/windows/app.ico` | 16, 24, 32, 48, 64, 128, 256 |
| macOS | `.icns` | `Assets/icons/macos/AppIcon.icns` | 16, 32, 64, 128, 256, 512, 1024 |
| Linux | `.png` (set) | `Assets/icons/linux/icon-*.png` | 16, 32, 64, 128, 256 |
| Kaynak | `.svg` veya buyuk `.png` | `Assets/source/app-icon.svg` | 1024x1024 veya daha buyuk |

## 2. Kaynak Dosya Stratejisi

1. Tek bir master icon kullan: tercihen vektor `SVG`, alternatif olarak 1024x1024 veya daha buyuk bir `PNG`.
2. Tum platform dosyalarini bu master dosyadan turet.
3. Path ve dosya isimleri sabit kalir; yalnizca dosya icerigi degisir.
4. Gecici export artefact'lari uretebilirsin, ancak release pipeline'inin tükettigi dosyalar sadece bu dokumandaki sabit yollardir.

## 3. Onerilen Boyutlar

1. Windows `.ico` icinde 16, 24, 32, 48, 64, 128 ve 256 px katmanlari bulunmali.
2. macOS `.icns` icinde 16'dan 1024 px'e kadar standart AppIcon seti bulunmali.
3. Linux icin minimum dosya seti sunlardir:
	- `Assets/icons/linux/icon-16.png`
	- `Assets/icons/linux/icon-32.png`
	- `Assets/icons/linux/icon-64.png`
	- `Assets/icons/linux/icon-128.png`
	- `Assets/icons/linux/icon-256.png`

## 4. Tasarim Ipuclari

1. Kucuk olcekte okunabilirlik oncelikli olsun; 16x16 boyutta ana sekil ayirt edilebilmeli.
2. Ince cizgilerden kacinin; 16x16 ve 32x32 varyantlarda stroke kalinligini artirmak gerekebilir.
3. Metin kullanmaktan kacinin; harfler kucuk boyutta hizla okunamaz hale gelir.
4. Acik ve koyu arka planlarda kontrasti test edin.
5. Ic bosluklar ve koseler cok dar olmamali; aksi halde ikon kucuk boyutta tek parca bir lekeye doner.

## 5. Teknik Ipuclari

1. Renk profili olarak `sRGB` kullanin.
2. Alpha kanali bozulmamis olmali.
3. PNG export sonrasinda gereksiz metadata temizlenebilir.
4. Windows `.ico` ve macOS `.icns` dosyalari tek dosya icinde coklu boyut seti icermeli.
5. Sıkıştırma agresifse kenarlarda bozulma yaratabilecegi icin export kalitesini kucuk boyutlarda kontrol edin.

## 6. Test Checklist

1. Pencere basligi iconu dogru gorunuyor mu?
2. Windows taskbar iconu net mi?
3. macOS dock iconu kirpilma olmadan merkezli gorunuyor mu?
4. Linux launcher iconu farkli tema ve olceklerde okunakli mi?
5. 100%, 125%, 150% ve 200% olceklerde kalite kabul edilebilir mi?

## 7. Bu Repodaki Sabit Yol Sozlesmesi

- Windows: `Assets/icons/windows/app.ico`
- macOS: `Assets/icons/macos/AppIcon.icns`
- Linux: `Assets/icons/linux/icon-16.png`, `icon-32.png`, `icon-64.png`, `icon-128.png`, `icon-256.png`
- Kaynak: `Assets/source/app-icon.svg`

Not: Gercek icon hazir geldiginde yalnizca bu dosyalarin icerigini guncellemek yeterlidir. CI ve publish tarafinda bu path'lerin degismedigi varsayilir.
