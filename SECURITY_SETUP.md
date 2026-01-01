# 🔐 Secrets Management - Kurulum Rehberi

## ✅ Yapılan Değişiklikler

### 1. `.env.example` Oluşturuldu ✓
Tüm gerekli environment variable'ların template'i oluşturuldu.

### 2. `.gitignore` Güncellendi ✓
`.env` dosyası ve varyasyonları git'e eklenmeyecek şekilde ayarlandı.

### 3. `docker-compose.yml` Güncellendi ✓
Hardcoded şifreler kaldırıldı, artık environment variable'lardan okuyor:
- `POSTGRES_PASSWORD=${DB_PASSWORD}`
- `ConnectionStrings__DefaultConnection=...Password=${DB_PASSWORD}`
- `ConnectionStrings__MongoDbConnection=...${MONGO_PASSWORD}...`
- `MONGO_INITDB_ROOT_PASSWORD=${MONGO_PASSWORD}`

---

## 🚀 Kurulum Adımları

### Adım 1: .env Dosyasını Oluştur

`.env.example` dosyasını kopyalayın:

```bash
cp .env.example .env
```

### Adım 2: Güçlü Şifreler Oluşturun

Aşağıdaki komutlardan birini kullanarak güçlü şifreler oluşturun:

**OpenAI kullanarak (Linux/Mac):**
```bash
openssl rand -base64 32
```

**Python kullanarak:**
```bash
python3 -c "import secrets; print(secrets.token_urlsafe(32))"
```

**PowerShell kullanarak (Windows):**
```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
```

### Adım 3: .env Dosyasını Doldurun

`.env` dosyasını düzenleyin ve aşağıdaki değişkenleri güncelleyin:

```bash
# Güçlü şifrelerinizi buraya girin (Adım 2'deki şifreleri kullanın)
DB_PASSWORD=oluşturduğunuz_postgres_şifresi
MONGO_PASSWORD=oluşturduğunuz_mongo_şifresi

# OpenAI API Key'inizi girin
OPENAI_API_KEY=sk-proj-xxxxx...

# Diğer ayarlar (isteğe bağlı)
LARGE_DATASET_THRESHOLD=10
CACHE_TTL_MINUTES=5
```

### Adım 4: Konteynerleri Yeniden Başlatın

Mevcut konteynerleri durdurun ve volume'ları silin (veriler silinecek, bu yüzden sadece ilk kurulumda yapın):

```bash
# Mevcut konteynerları durdur
docker-compose down

# Volume'ları sil (UYARI: Tüm veriler silinir!)
docker volume rm datawhisper-me-api_postgres_data datawhisper-me-api_mongodb_data

# Yeni environment variable'lar ile başlat
docker-compose up -d
```

**Alternatif: Mevcut verileri korumak için**

Eğer mevcut verilerinizi korumak istiyorsanız, şifreleri değiştirmeden önce database kullanıcılarının şifrelerini güncelleyin:

```bash
# PostgreSQL şifresini güncelle
docker-compose exec postgres psql -U datawhisper_user -c "ALTER USER datawhisper_user WITH PASSWORD 'yeni_şifre';"

# MongoDB şifresini güncelle
docker-compose exec mongodb mongosh --eval "db.changeUserPassword('datawhisper_user', 'yeni_şifre')"

# Sonra .env dosyasını güncelleyip restart yapın
docker-compose restart
```

---

## 🔍 Doğrulama

### Environment Variable'ların Yüklendiğini Kontrol Et

```bash
docker-compose config
```

Bu komut, çözümlenmiş configuration'ı gösterecek (şifreleriniz görünür olacak, bu yüzden paylaşmayın).

### Konteynerlerin Çalıştığını Kontrol Et

```bash
docker-compose ps
```

Tüm servislerin "Up" durumda olduğunu görmelisiniz.

### Database Bağlantısını Test Et

```bash
# PostgreSQL bağlantısını test et
docker-compose exec postgres psql -U datawhisper_user -d datawhisper -c "SELECT version();"

# MongoDB bağlantısını test et
docker-compose exec mongodb mongosh --username datawhisper_user --password --authenticationDatabase admin
```

---

## 🚨 Güvenlik Best Practices

### ✅ YAPILMASI GEREKENLER

1. **Güçlü Şifreler Kullanın**
   - En az 16 karakter
   - Büyük ve küçük harfler
   - Sayılar ve özel karakterler
   - Her database için farklı şifre

2. **Şifreleri Düzenli Rotation Yapın**
   - En az 3 ayda bir
   - Rotation yaparken yukarıdaki "Mevcut verileri korumak" adımlarını takip edin

3. **Production'da Farklı Şifreler Kullanın**
   - Development ve Production ortamları için ayrı `.env` dosyaları
   - Production için GitHub Secrets veya Docker Secrets kullanın

4. **.env Dosyasını Asla Commit Etmeyin**
   - `.gitignore` dosyasına zaten eklendi ✓
   - Yanlışlıkla commit edilirse: `git rm --cached .env && git commit -m "Remove .env"`

### ❌ YAPILMAMASI GEREKENLER

1. ❌ `.env` dosyasını git'e commit etmek
2. ❌ Aynı şifreyi birden fazla ortamda kullanmak
3. ❌ Zayıf şifreler (örn: "password123", "admin")
4. ❌ Şifreleri slack, discord veya e-postada paylaşmak
5. ❌ Production'da default şifreler kullanmak

---

## 🌐 Production Deployment

### GitHub Secrets Kullanımı

Production için `.env` dosyası yerine GitHub Secrets kullanın:

```yaml
# GitHub Actions workflow'da
env:
  DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
  MONGO_PASSWORD: ${{ secrets.MONGO_PASSWORD }}
  OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

GitHub Secrets eklemek için:
1. Repository → Settings → Secrets and variables → Actions
2. "New repository secret" tıklayın
3. Değişken adı ve değerini girin

### Docker Secrets Kullanımı (Swarm/Kubernetes)

Docker Swarm veya Kubernetes kullanıyorsanız:

```yaml
# docker-compose.yml (production)
version: '3.8'
services:
  postgres:
    secrets:
      - db_password
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password

secrets:
  db_password:
    external: true
```

---

## 📝 Environment Variable Referansı

| Değişken | Zorunlu? | Açıklama | Örnek Değer |
|----------|----------|----------|-------------|
| `DB_PASSWORD` | ✓ | PostgreSQL şifresi | `r@nD0mP@ssw0rd!2024` |
| `MONGO_PASSWORD` | ✓ | MongoDB şifresi | `m0ng0DBS3cur3P@ss!` |
| `OPENAI_API_KEY` | ✓ | OpenAI API key | `sk-proj-xxxxx...` |
| `AI_SERVICE_URL` | - | AI service URL | `http://datawhisper-ai:5003` |
| `LARGE_DATASET_THRESHOLD` | - | Büyük dataset eşiği | `10` |
| `CACHE_TTL_MINUTES` | - | Cache süresi (dakika) | `5` |
| `ASPNETCORE_ENVIRONMENT` | - | ASP.NET ortamı | `Development` / `Production` |
| `VERSION` | - | Docker image versiyonu | `latest` / `v1.0.0` |

---

## 🐛 Sorun Giderme

### Sorun: Konteynerler başlamıyor
```bash
# Logları kontrol et
docker-compose logs postgres
docker-compose logs mongodb
docker-compose logs datawhisper-api

# Çözüm: Şifrelerin .env dosyasında doğru olduğunu kontrol edin
```

### Sorun: "Password authentication failed" hatası
```bash
# Çözüm: Database şifrelerini .env dosyasındakiyle eşleştirin
docker-compose down -v
docker-compose up -d
```

### Sorun: Environment variable yüklenmemiş
```bash
# Docker'ın .env dosyasını okuduğunu kontrol et
docker-compose config | grep PASSWORD

# Çözüm: .env dosyasının proje root dizininde olduğunu kontrol edin
```

---

## ✅ Kurulum Tamamlandı

Artık tüm şifreleriniz güvenli bir şekilde saklanıyor ve git'e commit edilmiyor.

**Sonraki adım:** SECURITY_TODO.md dosyasındaki bir sonraki güvenlik görevini tamamlayın!
