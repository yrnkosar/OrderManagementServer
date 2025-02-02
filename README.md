# 🖥 Backend

## 📋 Genel Bakış

Bu proje, ASP.NET Core Web API kullanarak geliştirilmiş backend uygulamasıdır. Kullanıcı doğrulama, sipariş yönetimi ve stok kontrol mekanizmalarını içerir.

## 🚀 Özellikler

✅ Kullanıcı kimlik doğrulama (JWT)

✅ Ürün ekleme, silme ve güncelleme (Admin)

✅ Sipariş oluşturma ve işleme

✅ Gerçek zamanlı bildirimler (SignalR ile)

✅ Dinamik öncelik sıralaması ve log kayıtları

## 🛠 Kullanılan Teknolojiler

C# & ASP.NET Core Web API

Entity Framework Core (EF Core)

SQL Server

SignalR (Gerçek zamanlı bildirimler)

Multithreading & Semaphore (Eş zamanlı işlemler için)

## 🔧 Kurulum ve Çalıştırma

Bağımlılıkları yükleyin:
```
dotnet restore
```
Veritabanı bağlantısını appsettings.json dosyasında tanımlayın:
```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=OrderManagement;Trusted_Connection=True;"
  }
}
```

Veritabanı migrasyonlarını çalıştırın:
```
dotnet ef database update
```
Sunucuyu başlatmak için:
```
dotnet run
```
Varsayılan olarak ```http://localhost:5000``` adresinde çalışır.

### 🔗 API Endpoints

| Yöntem  | Endpoint                        | Açıklama                             |
|---------|--------------------------------|---------------------------------|
| **POST** | `/api/auth/login`             | Kullanıcı girişi                 |
| **POST** | `/api/auth/register`          | Yeni kullanıcı kaydı               |
| **GET**  | `/api/products`               | Tüm ürünleri getir                  |
| **POST** | `/api/orders/place-order`      | Sipariş oluştur                    |
| **GET**  | `/api/orders/my-orders`       | Kullanıcının siparişlerini getir  |
| **POST** | `/api/orders/approve-all-orders` | Tüm siparişleri admin tarafından onayla |

📜 Lisans

Bu proje, MIT Lisansı altında sunulmuştur.
