# MinimalApiJwtDemo

یک پروژه نمونه با **Minimal API** در **.NET 10** که موارد زیر را پیاده‌سازی می‌کند:

- احراز هویت با **JWT Bearer**
- مستندسازی با **Swashbuckle.AspNetCore** (بدون هیچ وابستگی به `Microsoft.AspNetCore.OpenApi` یا `.WithOpenApi()`)
- دکمه **Authorize** در Swagger UI برای وارد کردن توکن
- مدل‌های ورودی/خروجی به صورت `record`
- سه اندپوینت نمونه: ورود (`login`)، دریافت اطلاعات کاربر (`me`) و ارسال داده به یک سرویس خارجی با `IHttpClientFactory`

## چرا فقط Swashbuckle؟

از نسخه ۱۰ به بعد، `Swashbuckle.AspNetCore` به‌جای فضای نام قدیمی `Microsoft.OpenApi.Models` از فضای نام جدید
`Microsoft.OpenApi` استفاده می‌کند (سازگار با OpenAPI 3.1 / `Microsoft.OpenApi` نسخه ۲). این پکیج کاملاً خودکفاست و
نیازی به نصب پکیج `Microsoft.AspNetCore.OpenApi` ندارد. اگر آن پکیج را هم اضافه کنید، ممکن است بین نسخه‌های
`Microsoft.OpenApi` که هرکدام به آن نیاز دارند تداخل ایجاد شود؛ به همین دلیل در این پروژه **فقط** `Swashbuckle.AspNetCore`
نصب شده و از متد `.WithOpenApi()` هم استفاده نشده است.

## پیش‌نیازها

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## اجرا

```bash
cd MinimalApiJwtDemo
dotnet restore
dotnet run
```

سپس مرورگر را به آدرس زیر باز کنید (پورت را بر اساس خروجی ترمینال تنظیم کنید):

```
http://localhost:5080/swagger
```

## تنظیمات JWT

کلید امضای توکن در `appsettings.json` (بخش `Jwt:SigningKey`) قرار دارد. این مقدار فقط برای توسعه/دمو است.
برای محیط واقعی حتماً آن را جایگزین کنید، مثلاً با:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "یک-کلید-تصادفی-حداقل-۳۲-کاراکتری"
```

یا با متغیر محیطی `Jwt__SigningKey` در سرور production.

## کاربران نمونه (فقط برای دمو)

| Username | Password    | Roles       |
|----------|-------------|-------------|
| admin    | P@ssw0rd!   | Admin, User |
| ali      | 12345678    | User        |

> در پروژه واقعی حتماً از یک user store واقعی (مثل ASP.NET Core Identity + دیتابیس) با پسورد هش‌شده استفاده کنید.

## تست اندپوینت‌ها

### ۱. دریافت توکن

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"P@ssw0rd!"}'
```

پاسخ شامل `token` و `expiresAtUtc` خواهد بود.

### ۲. دریافت اطلاعات کاربر (نیازمند توکن)

```bash
curl http://localhost:5080/api/users/me \
  -H "Authorization: Bearer <TOKEN>"
```

### ۳. ارسال داده (فراخوانی سرویس خارجی از طریق IHttpClientFactory)

```bash
curl -X POST http://localhost:5080/api/data/send \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"title":"تست","payload":"این یک محموله نمونه است"}'
```

این اندپوینت مقدار ارسالی را به یک API عمومی و رایگان (`jsonplaceholder.typicode.com`) به‌عنوان شبیه‌سازی یک
سرویس بیرونی (مثلاً وب‌سرویس یک اپراتور/پارتنر) فوروارد می‌کند و نتیجه را همراه با شناسه مرجع بیرونی برمی‌گرداند.
در پروژه واقعی، `client.BaseAddress` را در `Program.cs` به آدرس سرویس واقعی خودتان تغییر دهید.

### استفاده از Swagger UI

1. روی دکمه **Authorize** (بالای صفحه Swagger) کلیک کنید.
2. فقط مقدار خام توکن را وارد کنید (بدون پیشوند `Bearer `؛ Swagger خودش اضافه می‌کند).
3. اندپوینت‌های محافظت‌شده (`/api/users/me` و `/api/data/send`) را مستقیماً از داخل Swagger UI فراخوانی کنید.

## ساختار پروژه

```
MinimalApiJwtDemo/
├── MinimalApiJwtDemo.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Properties/
│   └── launchSettings.json
└── README.md
```
