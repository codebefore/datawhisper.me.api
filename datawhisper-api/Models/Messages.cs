namespace DataWhisper.API.Models
{
    public static class Messages
    {
        public const string Turkish = "tr";
        public const string English = "en";
        public const string DefaultLanguage = English;

        // API Messages
        public static class Api
        {
            // Success Messages
            public static readonly Dictionary<string, string> QuerySuccess = new()
            {
                [Turkish] = "Sorgu başarıyla çalıştırıldı",
                [English] = "Query executed successfully"
            };

            public static readonly Dictionary<string, string> ApiRunning = new()
            {
                [Turkish] = "DataWhisper API çalışıyor!",
                [English] = "DataWhisper API is running!"
            };

            public static readonly Dictionary<string, string> DatabaseConnected = new()
            {
                [Turkish] = "Veritabanına başarıyla bağlandı",
                [English] = "Database connected successfully"
            };

            public static readonly Dictionary<string, string> SqlGenerated = new()
            {
                [Turkish] = "SQL DataWhisper ile oluşturuldu",
                [English] = "SQL generated using DataWhisper"
            };

            public static readonly Dictionary<string, string> PatternMatchUsed = new()
            {
                [Turkish] = "SQL pattern matching ile oluşturuldu (fallback)",
                [English] = "SQL generated using pattern matching (fallback)"
            };

            // Error Messages
            public static readonly Dictionary<string, string> InvalidRequest = new()
            {
                [Turkish] = "Geçersiz istek",
                [English] = "Invalid request"
            };

            public static readonly Dictionary<string, string> ServiceUnavailable = new()
            {
                [Turkish] = "AI servisi kullanılamıyor - SQL oluşturulamıyor",
                [English] = "AI service unavailable - cannot generate SQL"
            };

            public static readonly Dictionary<string, string> QueryExecutionFailed = new()
            {
                [Turkish] = "Sorgu çalıştırma başarısız oldu",
                [English] = "Query execution failed"
            };

            public static readonly Dictionary<string, string> EmptyPrompt = new()
            {
                [Turkish] = "Prompt boş olamaz",
                [English] = "Prompt cannot be empty"
            };

            public static readonly Dictionary<string, string> DatabaseConnectionFailed = new()
            {
                [Turkish] = "Veritabanı bağlantısı başarısız",
                [English] = "Database connection failed"
            };

            // Pagination Validation Messages
            public static readonly Dictionary<string, string> InvalidPageNumber = new()
            {
                [Turkish] = "Sayfa numarası 0'dan büyük olmalıdır",
                [English] = "Page number must be greater than 0"
            };

            public static readonly Dictionary<string, string> InvalidPageSize = new()
            {
                [Turkish] = "Sayfa boyutu 1 ile 1000 arasında olmalıdır",
                [English] = "Page size must be between 1 and 1000"
            };

            // AI Service Messages
            public static readonly Dictionary<string, string> AiServiceNotConfigured = new()
            {
                [Turkish] = "AI servisi yapılandırılmadı - SQL oluşturulamıyor",
                [English] = "AI service not available - cannot generate SQL"
            };

            public static readonly Dictionary<string, string> AiServiceError = new()
            {
                [Turkish] = "AI servisi hatası - lütfen servis durumunu kontrol edin",
                [English] = "AI service failed - please check service status"
            };
        }

        // UI Messages
        public static class Ui
        {
            // Navigation
            public static readonly Dictionary<string, string> Dashboard = new()
            {
                [Turkish] = "Panel",
                [English] = "Dashboard"
            };

            public static readonly Dictionary<string, string> QueryHistory = new()
            {
                [Turkish] = "Sorgu Geçmişi",
                [English] = "Query History"
            };

            public static readonly Dictionary<string, string> Analytics = new()
            {
                [Turkish] = "Analitik",
                [English] = "Analytics"
            };

            // Query Form
            public static readonly Dictionary<string, string> EnterYourQuestion = new()
            {
                [Turkish] = "Sorunuzu yazın...",
                [English] = "Enter your question..."
            };

            public static readonly Dictionary<string, string> AskQuestion = new()
            {
                [Turkish] = "Sor",
                [English] = "Ask"
            };

            public static readonly Dictionary<string, string> Clear = new()
            {
                [Turkish] = "Temizle",
                [English] = "Clear"
            };

            // Results
            public static readonly Dictionary<string, string> QueryResults = new()
            {
                [Turkish] = "Sorgu Sonuçları",
                [English] = "Query Results"
            };

            public static readonly Dictionary<string, string> GeneratedSql = new()
            {
                [Turkish] = "Oluşturulan SQL",
                [English] = "Generated SQL"
            };

            public static readonly Dictionary<string, string> ExecutionTime = new()
            {
                [Turkish] = "Çalışma Süresi",
                [English] = "Execution Time"
            };

            public static readonly Dictionary<string, string> RowCount = new()
            {
                [Turkish] = "Satır Sayısı",
                [English] = "Row Count"
            };

            // Chart Views
            public static readonly Dictionary<string, string> TableView = new()
            {
                [Turkish] = "Tablo Görünümü",
                [English] = "Table View"
            };

            public static readonly Dictionary<string, string> ChartView = new()
            {
                [Turkish] = "Grafik Görünümü",
                [English] = "Chart View"
            };

            // Loading States
            public static readonly Dictionary<string, string> GeneratingQuery = new()
            {
                [Turkish] = "Sorgu oluşturuluyor...",
                [English] = "Generating query..."
            };

            public static readonly Dictionary<string, string> ExecutingQuery = new()
            {
                [Turkish] = "Sorgu çalıştırılıyor...",
                [English] = "Executing query..."
            };

            public static readonly Dictionary<string, string> LoadingResults = new()
            {
                [Turkish] = "Sonuçlar yükleniyor...",
                [English] = "Loading results..."
            };

            // Error Messages
            public static readonly Dictionary<string, string> SomethingWentWrong = new()
            {
                [Turkish] = "Bir hata oluştu",
                [English] = "Something went wrong"
            };

            public static readonly Dictionary<string, string> TryAgain = new()
            {
                [Turkish] = "Tekrar deneyin",
                [English] = "Try again"
            };

            // History
            public static readonly Dictionary<string, string> NoPreviousQueries = new()
            {
                [Turkish] = "Daha önce sorgu bulunmuyor",
                [English] = "No previous queries found"
            };

            public static readonly Dictionary<string, string> ReRunQuery = new()
            {
                [Turkish] = "Sorguyu Tekrar Çalıştır",
                [English] = "Re-run Query"
            };

            // Analytics
            public static readonly Dictionary<string, string> TotalQueries = new()
            {
                [Turkish] = "Toplam Sorgu",
                [English] = "Total Queries"
            };

            public static readonly Dictionary<string, string> SuccessRate = new()
            {
                [Turkish] = "Başarı Oranı",
                [English] = "Success Rate"
            };

            public static readonly Dictionary<string, string> AverageExecutionTime = new()
            {
                [Turkish] = "Ortalama Çalışma Süresi",
                [English] = "Average Execution Time"
            };

            public static readonly Dictionary<string, string> TotalCost = new()
            {
                [Turkish] = "Toplam Maliyet",
                [English] = "Total Cost"
            };
        }

        // Helper method to get message in specific language
        public static string GetMessage(Dictionary<string, string> messages, string language = DefaultLanguage)
        {
            return messages.TryGetValue(language, out var message)
                ? message
                : messages.TryGetValue(DefaultLanguage, out var defaultMsg)
                    ? defaultMsg
                    : messages.Values.FirstOrDefault() ?? "Message not found";
        }
    }
}