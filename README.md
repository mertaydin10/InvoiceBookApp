# FaturaDefteri

Serbest çalışan / küçük işletme için **yerel fatura defteri**. ASP.NET Core (`net10`) + SQLite. Docker veya Postgres gerekmez.

Tek proje: `src/FaturaDefteri.Api`. Arayüz `wwwroot`.

## Ne işe yarar

- Firma bilgisi (unvan, VKN, IBAN) — yazdırılan faturanın üstü
- Müşteri kartı
- Fatura: kalem, miktar, birim fiyat, KDV
- Numara: `FAT-2026-001`
- Durum: taslak → gönderildi → ödendi / iptal; vadesi geçmiş gönderilmiş fatura **gecikmiş**
- Özet: açık tutar, gecikmiş, bu ay tahsilat
- Yazdırılabilir fatura (tarayıcı yazdır)

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

| | |
| --- | --- |
| `GET /health` | DB |
| `GET` `PUT /api/issuer` | firma |
| `/api/clients` | CRUD |
| `POST /api/invoices` | taslak |
| `PUT /api/invoices/{id}` | taslak/gönderildi düzenle |
| `POST .../send` `pay` `cancel` | durum |
| `GET /api/invoices?status=&clientId=` | `overdue` filtresi var |
| `GET /api/stats/summary` | özet |
