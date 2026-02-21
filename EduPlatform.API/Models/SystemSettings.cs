namespace EduPlatform.API.Models
// Models/SystemSettings.cs

{
    public class SystemSettings
    {
        public int Id { get; set; }
        public string SiteName { get; set; }
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string WelcomeMessage { get; set; }
        public bool EnableRegistration { get; set; }
        public bool EnableCoursePurchase { get; set; }
        public bool MaintenanceMode { get; set; }
        public string Currency { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class UpdateSettingsDto
    {
        public string SiteName { get; set; }
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string WelcomeMessage { get; set; }
        public bool EnableRegistration { get; set; }
        public bool EnableCoursePurchase { get; set; }
        public bool MaintenanceMode { get; set; }
        public string Currency { get; set; }
        public bool EnableEmailNotifications { get; set; }
    }
}