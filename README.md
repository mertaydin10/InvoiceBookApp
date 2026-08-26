# FaturaDefteri

Serbest çalışan / küçük işletme için **yerel fatura defteri**. ASP.NET Core (`net10`) + SQLite. Docker veya Postgres gerekmez.

Tek proje: `src/FaturaDefteri.Api`. Arayüz `wwwroot`.

## Ne işe yarar

- **Kullanıcı yönetimi**: Kayıt, giriş, profil düzenleme, şifre değiştirme
- **Firma bilgisi**: Unvan, VKN, IBAN, para birimi seçimi — yazdırılan faturanın üstü
- **Müşteri kartı**: Müşteri bilgileri, vergi numarası, iletişim
- **Fatura**: Kalem, miktar, birim fiyat, KDV
- **Numara**: `FAT-2026-001` formatında otomatik fatura numarası
- **Durum**: Taslak → Gönderildi → Ödendi / İptal; vadesi geçmiş gönderilmiş fatura **gecikmiş**
- **Filtreleme**: Durum, müşteri, tarih aralığına göre fatura filtreleme
- **CSV dışa aktarma**: Fatura listesini CSV olarak indirme
- **Özet dashboard**: Açık tutar, gecikmiş, bu ay tahsilat, aylık gelir grafiği
- **Yazdırılabilir fatura**: Tarayıcı ile yazdırma
- **Çoklu para birimi**: TRY, USD, EUR, GBP ve daha fazlası

Bu bir e-fatura / GİB entegrasyonu değildir; tahsilatı ve müşteri bakiyesini takip etmek içindir.

## Çalıştırma

```bash
dotnet run --project src/FaturaDefteri.Api
```

- Uygulama: [http://localhost:5020](http://localhost:5020)
- Swagger (Development): [http://localhost:5020/swagger](http://localhost:5020/swagger)
- SQLite dosyası: `src/FaturaDefteri.Api/data/fatura.db`

## Test

```bash
dotnet test
```

## API

| Endpoint | Açıklama |
| --- | --- |
| `GET /health` | Veritabanı durumu |
| **Kimlik doğrulama** | |
| `POST /api/auth/register` | Yeni kullanıcı kaydı |
| `POST /api/auth/login` | Giriş yap, JWT token al |
| `GET /api/auth/me` | Kullanıcı bilgileri |
| `PUT /api/auth/me` | Profil güncelle |
| `POST /api/auth/change-password` | Şifre değiştir |
| **Para birimleri** | |
| `GET /api/currencies` | Desteklenen para birimleri |
| **Firma** | |
| `GET` `PUT /api/issuer` | Firma bilgileri |
| **Müşteriler** | |
| `GET` `POST /api/clients` | Liste, yeni müşteri |
| `PUT` `DELETE /api/clients/{id}` | Güncelle, sil |
| **Faturalar** | |
| `GET /api/invoices` | Liste (filtreleme: status, clientId, fromDate, toDate) |
| `POST /api/invoices` | Yeni fatura (taslak) |
| `GET /api/invoices/{id}` | Detay |
| `PUT /api/invoices/{id}` | Güncelle (taslak/gönderildi) |
| `POST /api/invoices/{id}/send` | Gönderildi olarak işaretle |
| `POST /api/invoices/{id}/pay` | Ödendi |
| `POST /api/invoices/{id}/cancel` | İptal et |
| `DELETE /api/invoices/{id}` | Sil (sadece taslak) |
| **İstatistikler** | |
| `GET /api/stats/summary` | Özet (müşteri, açık fatura, gecikmiş, tahsilat) |
| `GET /api/stats/monthly-revenue` | Son 6 ay aylık tahsilat |

## Özellikler

- ✅ JWT kimlik doğrulama
- ✅ Kullanıcı bazlı veri izolasyonu
- ✅ Çoklu para birimi desteği
- ✅ Tarih aralığı filtreleme
- ✅ CSV dışa aktarma
- ✅ Aylık gelir grafiği
- ✅ Kullanıcı profil yönetimi
- ✅ Şifre değiştirme
- ✅ Aydınlık tema
- ✅ Responsive tasarım
