# Progress Bar Airport Mountain (GTA V Script)

Mod script untuk GTA V yang menambahkan tantangan perjalanan menuju target koordinat (dari koordinat awal ke tujuan akhir). Script ini memiliki beberapa fitur utama seperti *Progress Bar*, *Health Bar*, *Countdown* untuk garis finis, dan sistem *Win Counter*.

## Fitur-Fitur (Versi 1.0.8)
*   **Target Perjalanan**: Mencapai jarak tertentu (Airport ke Mountain) dengan indikator persentase dan sisa jarak (meter).
*   **Progress & Health Bar**: UI yang menarik untuk menampilkan progres perjalanan dan darah (Health) pemain secara langsung di layar.
*   **Configurable Win & Countdown**: Saat mencapai jarak `< 30 meter`, sistem akan memulai hitungan mundur (Countdown) sebelum mencetak *Win*. Durasi countdown dan jumlah kemenangan dapat dikonfigurasi melalui file `.ini`.
*   **Dynamic Win Teks**: Menampilkan tulisan "WIN" dengan desain dinamis di tengah layar (menggunakan *font* Pricedown GTA) beserta perayaan kembang api (Fireworks).
*   **Death Overlay (Wasted)**: Teks kematian "YAAHH TURU DEK!!!" jika pemain tewas di tengah perjalanan.
*   **Sistem Lisensi**: Hanya untuk pengguna terdaftar dengan sistem validasi *Hardware ID (HWID)*.

## Cara Pemasangan (Instalasi)
1. Pastikan kamu sudah menginstal **ScriptHookV** dan **ScriptHookVDotNet3** di GTA V kamu.
2. Download file rilis `AirportMountain.dll` dari halaman [Releases](https://github.com/tamfansdanberani/progressbar-airportmountain-dll/releases).
3. Pindahkan file `.dll` tersebut ke dalam folder `scripts` di dalam direktori utama game GTA V milikmu.
   > *Contoh: `C:\Program Files\Rockstar Games\Grand Theft Auto V\scripts\`*
4. Jalankan game. Script akan dimuat secara otomatis.

## Konfigurasi (.ini)
Pertama kali script dijalankan, file `AirportMountain.ini` akan otomatis dibuat di folder `scripts`.
Kamu bisa mengedit file ini menggunakan Notepad. Berikut contoh isi pengaturannya:
```ini
Alias=User-1234
ShowPercentage=True
HID=xxxxxxxxxxxxxxxxxxxx
DeathText=YAAHH TURU DEK!!!
CountdownDuration=5
WinCount=0
```
*   `CountdownDuration`: Lama waktu (dalam detik) sebelum sistem mengesahkan kemenanganmu saat berada di area finis.
*   `WinCount`: Jumlah seberapa banyak kamu memenangkan tantangan ini (dapat diedit secara manual).
*   `DeathText`: Ubah kata-kata ketika pemain mati.

> **⚠️ PERINGATAN PENTING**: Jangan pernah mengubah atau menghapus isi dari `Alias` dan `HID`! Kedua parameter tersebut merupakan identitas unik yang terikat dengan lisensi PC/perangkatmu. Apabila nilainya diubah, script akan mendeteksinya sebagai tindakan curang (cheating) dan sistem mod akan otomatis diblokir.

## Catatan Tambahan
*   Saat update ke versi terbaru pastikan kamu men-delete file `.old` atau `.new` yang tertinggal jika sebelumnya kamu menggunakan sistem *auto-update*.
*   Mod ini terkoneksi dengan internet untuk sistem pengecekan lisensinya.

***
*Dibuat oleh Tamsfinity.*
