# 🔐 DataWhisper API - Güvenlik İyileştirme Listesi

Bu dosyadaki görevleri sırayla tamamlayarak projenin güvenliğini artırabilirsiniz. Bir görevi tamamladıktan sonra `[ ]` işaretini `[x]` olarak değiştirin.

---

## 🚨 ACİL GÖREVLER (Kritik - Hemen Yapılmalı)

### 1. Authentication - JWT Token Sistemi
- [ ] JWT authentication middleware ekle
- [ ] Kullanıcı login/register endpoint'leri oluştur
- [ ] Token generation ve validation implementasyonu
- [ ] Refresh token mekanizması
- [ ] Password hashing (bcrypt/Argon2)
- [ ] Tüm endpoint'lere `[Authorize]` attribute'ı ekle

**Dosyalar:**
- `Program.cs` - Authentication configuration
- `Controllers/AuthController.cs` - Yeni dosya
- `Models/User.cs` - Yeni dosya
- `Services/TokenService.cs` - Yeni dosya

---

### 2. SQL Injection Koruması
- [ ] AI tarafından oluşturulan SQL sorgularını validate et
- [ ] SQL syntax whitelist oluştur (SELECT, FROM, WHERE, JOIN, LIMIT vb.)
- [ ] Dangerous keywords blacklist (DROP, DELETE, TRUNCATE, ALTER, EXEC vb.)
- [ ] Query parameter sanitization
- [ ] SQL execution timeout limiti
- [ ] Log AI responses for audit

**Dosyalar:**
- `Controllers/QueryController.cs:410` - SQL execution point
- `Services/SqlValidationService.cs` - Yeni dosya
- `Utils/SqlSafeExecutor.cs` - Yeni dosya

---

### 3. Secrets Management
- [x] `.env.example` dosyası oluştur
- [x] `.gitignore`'a `.env` ekle
- [x] `docker-compose.yml`'den hardcoded şifreleri kaldır
- [x] Environment variable reference'larını ekle
- [x] GitHub Secrets kullanımı dokümantasyonu
- [x] Connection string'leri appsettings.json'dan kaldır

**Dosyalar:**
- `docker-compose.yml` - Environment variables
- `.env.example` - Template oluştur
- `.gitignore` - .env ekle
- `appsettings.json` - Connection strings sil

---

### 4. Security Headers Middleware
- [ ] NLog.SecurityHeaders paketini yükle (veya custom middleware)
- [ ] X-Frame-Options: DENY veya SAMEORIGIN
- [ ] X-Content-Type-Options: nosniff
- [ ] X-XSS-Protection: 1; mode=block
- [ ] Strict-Transport-Security (HSTS)
- [ ] Content-Security-Policy
- [ ] Referrer-Policy: no-referrer
- [ ] Permissions-Policy

**Dosyalar:**
- `Program.cs` - Middleware ekle
- `Middleware/SecurityHeadersMiddleware.cs` - Yeni dosya (opsiyonel)

---

### 5. Input Validation
- [ ] Prompt uzunluğu limiti (max 1000 karakter)
- [ ] Special character validation
- [ ] Null ve empty check
- [ ] Request body size limit
- [ ] AI response validation (json format kontrolü)
- [ ] FluentValidation paketini yükle

**Dosyalar:**
- `Models/QueryRequest.cs` - Validation attributes
- `Validators/QueryRequestValidator.cs` - Yeni dosya
- `Program.cs` - FluentValidation registration

---

## 📌 YÜKSEK ÖNCELİKLİ GÖREVLER

### 6. Rate Limiting Aktifleştirme
- [ ]AspNetCoreRateLimit paketini yükle
- [ ] appsettings.json'da rate limit rules konfigürasyonu
- [ ] IpRateLimiting middleware'i aktifleştir
- [ ] Token-based rate limiting (optional)
- [ ] Rate limit exceeded response format

**Dosyalar:**
- `appsettings.json` - Rate limit rules
- `Program.cs` - Rate limiting setup
- `.csproj` - Paket referansı

---

### 7. Authorization & Roles
- [ ] Role enum'ları oluştur (Admin, User, Guest)
- [ ] `[Authorize(Roles = "Admin")]` attribute'ları
- [ ] Policy-based authorization
- [ ] Resource-based authorization (kendi query'leri)
- [ ] Admin endpoint'leri (user management, system config)

**Dosyalar:**
- `Models/Role.cs` - Yeni dosya
- `Program.cs` - Authorization policies
- `Controllers/AdminController.cs` - Yeni dosya

---

### 8. Connection String Endpoint Kaldırma
- [ ] `/api/system/db-config` endpoint'ini disable et veya sil
- [ ] Connection string bilgilerini log'lardan temizle
- [ ] Sensitive data masking implementasyonu

**Dosyalar:**
- `Controllers/SystemController.cs:70-79` - Endpoint sil/disable
- `Middleware/SanitizeLoggingMiddleware.cs` - Yeni dosya

---

### 9. Swagger Güncelleme
- [ ] Swashbuckle.AspNetCore 6.4.0 → 6.5.x veya 7.x
- [ ] Security definitions ekle (JWT bearer)
- [ ] Swagger UI'da authorize butonu
- [ ] Production'da Swagger'ı disable et

**Dosyalar:**
- `DataWhisper.API.csproj` - Package update
- `Program.cs` - Swagger configuration

---

### 10. Docker Güvenliği
- [ ] Dockerfile'da non-root user oluştur
- [ ] Container health check ekle
- [ ] Resource limits (CPU, memory)
- [ ] Docker image scanning (Trivy)
- [ ] Base image version pin (8.0 → 8.0.X)

**Dosyalar:**
- `Dockerfile` - USER instruction, HEALTHCHECK
- `docker-compose.yml` - deploy: resources
- `.github/workflows/deploy.yml` - Trivy scan

---

## 📌 ORTA ÖNCELİKLİ GÖREVLER

### 11. CORS Production Configuration
- [ ] Environment-specific CORS policy
- [ ] Production origin whitelist
- [ ] `AllowAnyHeader()` kaldır, spesifik header'lar
- [ ] Preflight request caching

**Dosyalar:**
- `Program.cs` - CORS policy
- `appsettings.Production.json` - Production origins

---

### 12. Audit Logging
- [ ] Security event logger (failed logins, suspicious queries)
- [ ] User action logging (who ran what query)
- [ ] Audit log storage (separate database/file)
- [ ] Log retention policy
- [ ] Audit log dashboard/viewer

**Dosyalar:**
- `Services/AuditLogService.cs` - Yeni dosya
- `Middleware/AuditLogMiddleware.cs` - Yeni dosya
- `Models/AuditLog.cs` - Yeni dosya

---

### 13. Security Scanning CI/CD
- [ ] Trivy vulnerability scanning
- [ ] Snyk veya GitHub Dependabot
- [ ] OWASP ZAP veya Burp Suite testleri
- [ ] Security test pipeline

**Dosyalar:**
- `.github/workflows/security-scan.yml` - Yeni dosya
- `.trivy.yml` - Konfigürasyon

---

### 14. HTTPS Enforcement
- [ ] Strict HTTPS redirect
- [ ] HTTP to HTTPS redirect
- [ ] SSL/TLS certificate configuration
- [ ] HTTPS only in production

**Dosyalar:**
- `Program.cs` - HTTPS redirection
- `nginx.conf` veya reverse proxy config

---

### 15. API Resource Limits
- [ ] Max request body size (10-50 MB)
- [ ] Query execution timeout
- [ ] Concurrent request limit per user
- [ ] Database connection pool limits

**Dosyalar:**
- `Program.cs` - Request size limits
- `appsettings.json` - Limits configuration

---

### 16. Error Handling & Information Disclosure
- [ ] Generic error messages (stack trace gizle)
- [ ] Development vs Production error detail
- [ ] Exception filtering (sensitive data masking)
- [ ] Custom error response format

**Dosyalar:**
- `Middleware/ExceptionHandlerMiddleware.cs` - Güncelle
- `Models/ErrorResponse.cs` - Yeni dosya

---

### 17. Database Security
- [ ] Database user privileges azalt (least privilege)
- [ ] Separate read/write users
- [ ] Query execution user sandbox'ı
- [ ] Database connection encryption
- [ ] MongoDB authentication enable et

**Dosyalar:**
- `docker-compose.yml` - DB users configuration
- `Program.cs` - Separate connection strings

---

### 18. MongoDB Authentication
- [ ] MongoDB auth enable et
- [ ] Analytics collection access control
- [ ] Query history user isolation
- [ ] Connection string auth credentials

**Dosyalar:**
- `docker-compose.yml` - MONGO_INITDB_ROOT_*
- `Program.cs` - MongoDB auth settings

---

## 📌 DÜŞÜK ÖNCELİKLİ GÖREVLER

### 19. API Versioning
- [ ] URL path versioning (/api/v1/query)
- [ ] Version deprecation policy
- [ ] Multiple version support

**Dosyalar:**
- `Program.cs` - API versioning
- Controllers - Route attributes güncelle

---

### 20. OpenAPI/Swagger Security
- [ ] JWT Bearer authentication ekle
- [ ] API Key authentication (opsiyonel)
- [ ] OAuth2/Authorization Code flow (opsiyonel)

**Dosyalar:**
- `Program.cs` - Swagger security definitions

---

### 21. Monitoring & Alerts
- [ ] Failed login attempt alerts
- [ ] Rate limit exceeded notifications
- [ ] Suspicious query detection
- [ ] Application performance monitoring

**Dosyalar:**
- `Services/AlertService.cs` - Yeni dosya
- Monitoring dashboard (Grafana/Prometheus)

---

### 22. Backup & Recovery
- [ ] Database backup automation
- [ ] Backup encryption
- [ ] Disaster recovery plan
- [ ] Backup restore testing

**Dosyalar:**
- `scripts/backup.sh` - Yeni dosya
- CI/CD backup pipeline

---

## 📋 EKSTRA GÜVENLİK İYİLEŞTİRMELERİ

### 23. GDPR Compliance
- [ ] IP address logging consent mekanizması
- [ ] Data retention policy implementasyonu
- [ ] Right to deletion endpoint
- [ ] Privacy policy ve terms of service
- [ ] Cookie consent tracking

---

### 24. API Documentation Security
- [ ] Authentication required for Swagger
- [ ] API key in documentation
- [ ] Security best practices guide
- [ ] Rate limiting documentation

---

### 25. Penetration Testing
- [ ] OWASP ZAP scan
- [ ] Manual penetration test
- [ ] SQL injection test cases
- [ ] XSS test cases
- [ ] CSRF test cases

---

## 📊 İLERLEME TAKİBİ

**Görev Özeti:**
- [ ] ACİL: 0/5 tamamlandı
- [ ] YÜKSEK: 0/5 tamamlandı
- [ ] ORTA: 0/8 tamamlandı
- [ ] DÜŞÜK: 0/4 tamamlandı
- [ ] EKSTRA: 0/3 tamamlandı

**Toplam İlerleme:** 0/25 (%0)

---

## 🔗 KAYNAKLAR

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP ASVS](https://owasp.org/www-project-application-security-verification-standard/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)

---

**Not:** Bir görevi tamamladıktan sonra bu dosyada ilgili kutuyu işaretleyin ve bir sonraki göreve geçin. Her görev için ayrı bir commit atmanızı öneririm.
