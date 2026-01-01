# 🔐 GitHub Secrets Kurulum Rehberi

Bu rehber, DataWhisper API projenizi production'a deploy etmek için gerekli GitHub Secrets'ların nasıl ayarlanacağını açıklar.

---

## 📋 Gerekli GitHub Secrets Listesi

Production deployment için aşağıdaki GitHub Secrets'ları eklemeniz gerekiyor:

### 🔑 Zorunlu Secrets (Server Erişimi)

| Secret Adı | Açıklama | Örnek |
|------------|----------|-------|
| `SERVER_HOST` | Production server IP adresi veya domain | `160.20.111.45` |
| `SERVER_USER` | SSH kullanıcısı | `root` veya `ubuntu` |
| `SSH_PRIVATE_KEY` | SSH private key | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `SERVER_PORT` | SSH portu (opsiyonel, default: 22) | `22` |

### 🔐 Zorunlu Secrets (Database & API)

| Secret Adı | Açıklama | Örnek Format |
|------------|----------|--------------|
| `PROD_DB_PASSWORD` | Production PostgreSQL şifresi | `r@nD0mP@ssw0rd!2024` |
| `PROD_MONGO_PASSWORD` | Production MongoDB şifresi | `m0ng0DBS3cur3P@ss!` |
| `PROD_OPENAI_API_KEY` | OpenAI API Key | `sk-proj-xxxxx...` |

---

## 🚀 Adım Adım Kurulum

### Adım 1: GitHub Repository Sayfasına Git

1. GitHub'da repository'nizi açın: https://github.com/codebefore/datawhisper.me.api
2. Üst menüden **Settings** > **Secrets and variables** > **Actions** tıklayın

### Adım 2: Yeni Secret Ekle

**"New repository secret"** butonuna tıklayın ve aşağıdaki secrets'ları sırayla ekleyin:

---

### 📡 Server Erişim Secrets

#### 1. SERVER_HOST
- **Name:** `SERVER_HOST`
- **Value:** Production server IP adresiniz
- **Örnek:** `160.20.111.45`

#### 2. SERVER_USER
- **Name:** `SERVER_USER`
- **Value:** SSH ile bağlanacağınız kullanıcı adı
- **Örnek:** `root` veya `ubuntu`

#### 3. SSH_PRIVATE_KEY

Bunu oluşturmak için:

**Eğer SSH anahtarınız yoksa, yeni bir tane oluşturun:**

```bash
# Local bilgisayarınızda (Mac/Linux)
ssh-keygen -t ed25519 -a 100 -C "github-actions-datawhisper" -f ~/.ssh/datawhisper_deploy

# Public key'i server'a ekleyin
ssh-copy-id -i ~/.ssh/datawhisper_deploy.pub root@160.20.111.45

# Veya manuel olarak:
cat ~/.ssh/datawhisper_deploy.pub
# Çıktıyı server'daki ~/.ssh/authorized_keys dosyasına ekleyin
```

**Private key'i kopyalayın:**

```bash
cat ~/.ssh/datawhisper_deploy
```

- **Name:** `SSH_PRIVATE_KEY`
- **Value:** Private key'in tamamını yapıştırın (başlangıç ve bitiş satırları dahil)

```
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAtBAAAA...
...tüm satırlar...
-----END OPENSSH PRIVATE KEY-----
```

⚠️ **ÖNEMLİ:** Private key'i kopyalarken satır boşluklarını kaybetmeyin.

#### 4. SERVER_PORT (Opsiyonel)
- **Name:** `SERVER_PORT`
- **Value:** SSH portu (default 22 ise boş bırakabilirsiniz)
- **Örnek:** `22`

---

### 🔐 Database ve API Secrets

#### 5. PROD_DB_PASSWORD

Production PostgreSQL şifrenizi oluşturun:

```bash
# Güçlü şifre oluştur
openssl rand -base64 32
```

- **Name:** `PROD_DB_PASSWORD`
- **Value:** Oluşturduğunuz güçlü şifre

**Server'da PostgreSQL şifresini güncelleyin:**

```bash
# Server'a SSH ile bağlanın
ssh root@160.20.111.45

# PostgreSQL container'ına girin
docker exec -it datawhisper-db psql -U datawhisper_user -d datawhisper

# Şifreyi güncelleyin
ALTER USER datawhisper_user WITH PASSWORD 'GİRİLEN_ŞİFRE';
\q
```

#### 6. PROD_MONGO_PASSWORD

Production MongoDB şifrenizi oluşturun:

```bash
# Farklı bir şifre oluşturun
openssl rand -base64 32
```

- **Name:** `PROD_MONGO_PASSWORD`
- **Value:** Oluşturduğunuz güçlü şifre

**Server'da MongoDB şifresini güncelleyin:**

```bash
# MongoDB container'ına girin
docker exec -it datawhisper-mongodb mongosh

# Admin authentication
use admin
db.auth("datawhisper_user", "ESKİ_ŞİFRE")

# Şifreyi güncelleyin
db.changeUserPassword("datawhisper_user", "YENİ_ŞİFRE")
exit
```

#### 7. PROD_OPENAI_API_KEY

OpenAI API key'inizi girin:

- **Name:** `PROD_OPENAI_API_KEY`
- **Value:** OpenAI API key'iniz (`sk-proj-...` ile başlayan)

Bu key'i https://platform.openai.com/api-keys adresinden alabilirsiniz.

---

## ✅ Doğrulama

Tüm secrets'ları ekledikten sonra, GitHub Actions sekmesinden workflow'u manuel çalıştırabilirsiniz:

### Manuel Workflow Tetikleme

1. **Actions** sekmesine tıklayın
2. Soldan "Deploy API to Production" workflow'unu seçin
3. Sağ üstte "Run workflow" butonuna tıklayın
4. Branch: `main` seçin
5. **Run workflow** butonuna tıklayın

### Başarılı Çalışma Belirtileri

✅ Yeşil tik işareti
✅ "API deployment completed successfully!" mesajı
✅ Container status'ta "datawhisper-api" görünüyor

❌ Hata durumunda logları kontrol edin ve secret'ların doğru olduğunu doğrulayın.

---

## 🔒 Güvenlik Best Practices

### ✅ DO'S (Yapılacaklar)

1. **Farklı Şifreler Kullanın**
   - Production ve Development için ayrı şifreler
   - Her environment için farklı şifreler

2. **Güçlü Şifreler Oluşturun**
   - Minimum 16 karakter
   - Büyük/küçük harf, sayı, özel karakter
   - Kelime sözlüğü kullanmayın

3. **Düzenli Rotation**
   - Her 3-6 ayda bir şifreleri değiştirin
   - GitHub Secret'ları güncelleyin
   - Database şifrelerini değiştirin

4. **SSH Key Yönetimi**
   - Her ortam için farklı SSH key'leri
   - Key'leri parola ile koruyun
   - Eski key'leri iptal edin

### ❌ DON'TS (Yapılmayacaklar)

1. ❌ Secret'ları kod içinde commit etmeyin
2. ❌ Aynı şifreyi birden fazla yerde kullanmayın
3. ❌ Zayıf şifreler kullanmayın (örn: "password123", "admin")
4. ❌ Secret'ları Slack/Discord/e-posta ile paylaşmayın
5. ❌ Public repository'lerde production secrets kullanmayın

---

## 🔄 Secret Rotation (Şifre Değiştirme)

Şifreleri değiştirmek için:

### 1. Yeni Şifre Oluştur
```bash
openssl rand -base64 32
```

### 2. Database Şifresini Güncelle
```bash
# PostgreSQL
docker exec -it datawhisper-db psql -U datawhisper_user -d datawhisper
ALTER USER datawhisper_user WITH PASSWORD 'YENİ_ŞİFRE';

# MongoDB
docker exec -it datawhisper-mongodb mongosh
use admin
db.auth("datawhisper_user", "ESKİ_ŞİFRE")
db.changeUserPassword("datawhisper_user", "YENİ_ŞİFRE")
```

### 3. GitHub Secret'ı Güncelle
1. Repository Settings > Secrets and variables > Actions
2. ilgili secret'ı silin ve yeni değerini ekleyin
3. Eski secret'ı silmeyin, yeni bir tane oluşturun

### 4. Deployment Yapın
Workflow'u manuel çalıştırın veya yeni bir commit yapın.

---

## 🐛 Sorun Giderme

### Sorun: "Permission denied (publickey)" hatası

**Çözüm:**
1. SSH_PRIVATE_KEY'in doğru kopyalandığından emin olun
2. Public key'in server'da `~/.ssh/authorized_keys` dosyasında olduğunu kontrol edin
3. SSH permissions doğru mu: `chmod 700 ~/.ssh` ve `chmod 600 ~/.ssh/authorized_keys`

```bash
# Server'da kontrol et
ssh root@160.20.111.45 "cat ~/.ssh/authorized_keys"
```

### Sorun: "Password authentication failed" (PostgreSQL/MongoDB)

**Çözüm:**
1. GitHub Secret'lardaki şifrelerin doğru olduğunu kontrol edin
2. Database'deki şifrelerle eşleştiğini doğrulayın

```bash
# PostgreSQL test
docker exec -it datawhisper-db psql -U datawhisper_user -d datawhisper -c "SELECT version();"

# MongoDB test
docker exec -it datawhisper-mongodb mongosh --username datawhisper_user --password --authenticationDatabase admin
```

### Sorun: Container başlamıyor

**Çözüm:**
```bash
# Server'da container loglarını kontrol et
docker logs datawhisper-api --tail 50

# Connection string kontrolü
docker exec -it datawhisper-api env | grep ConnectionStrings
```

### Sorun: Environment variable yüklenmemiş

**Çözüm:**
1. GitHub Secret'ların isimlerinin doğru olduğunu kontrol edin (`PROD_DB_PASSWORD` vs `DB_PASSWORD`)
2. Workflow dosyasında `envs:` kısmında değişken adlarının doğru olduğunu doğrulayın

---

## 📊 Environment Farkı

### Development (.env)
```bash
DB_PASSWORD=dev_password_123
MONGO_PASSWORD=dev_mongo_123
OPENAI_API_KEY=sk-dev-xxxxx
```

### Production (GitHub Secrets)
```bash
PROD_DB_PASSWORD=prod_Str0ng_P@ssw0rd!
PROD_MONGO_PASSWORD=prod_M0ng0_S3cur3!
PROD_OPENAI_API_KEY=sk-proj-prod-xxxxx
```

⚠️ **ÖNEMLİ:** Development ve Production şifreleri farklı olmalı!

---

## ✅ Tüm Secrets Eklendiğinde

GitHub Secrets sayfanızda bu secret'ları görmelisiniz:

```
✅ SERVER_HOST
✅ SERVER_USER
✅ SSH_PRIVATE_KEY
✅ SERVER_PORT
✅ PROD_DB_PASSWORD
✅ PROD_MONGO_PASSWORD
✅ PROD_OPENAI_API_KEY
```

Artık her push'ta workflow otomatik çalışacak ve production'a güvenli bir şekilde deploy edecek!

---

## 🎯 Sonraki Adım

Tüm secrets'ları ekledikten sonra:

1. Test için bir commit yapın
2. GitHub Actions sekmesinden workflow'u izleyin
3. Başarılı deployment sonrası production API'nizi test edin

**Test komutu:**
```bash
curl http://160.20.111.45:8080/api/health
```

---

**Hazır!** 🎉 Artık GitHub Secrets ile güvenli deployment sisteminiz var.
