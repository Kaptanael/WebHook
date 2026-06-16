namespace MVPAPI.WebHook.Application.DTOs.Accounts;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}