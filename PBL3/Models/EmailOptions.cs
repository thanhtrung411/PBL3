namespace PBL3.Models;

public class EmailOptions
{
    public SmtpEmailOptions Smtp { get; set; } = new();
}

public class SmtpEmailOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "PBL3 Hotel";
    public bool EnableSsl { get; set; } = true;
}
