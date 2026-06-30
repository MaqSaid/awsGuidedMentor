namespace GuidedMentor.Identity.Application.DTOs;

/// <summary>
/// Internal result type returned from the Cognito auth service layer.
/// </summary>
public sealed class AuthResult
{
    public bool IsSuccess { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public string? IdToken { get; }
    public int ExpiresIn { get; }
    public string? UserId { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }

    private AuthResult(
        bool isSuccess,
        string? accessToken,
        string? refreshToken,
        string? idToken,
        int expiresIn,
        string? userId,
        string? errorMessage,
        string? errorCode)
    {
        IsSuccess = isSuccess;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IdToken = idToken;
        ExpiresIn = expiresIn;
        UserId = userId;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static AuthResult Success(
        string accessToken,
        string refreshToken,
        string idToken,
        int expiresIn,
        string? userId = null) =>
        new(true, accessToken, refreshToken, idToken, expiresIn, userId, null, null);

    public static AuthResult SuccessWithUserId(string userId) =>
        new(true, null, null, null, 0, userId, null, null);

    public static AuthResult Verified() =>
        new(true, null, null, null, 0, null, null, null);

    public static AuthResult Failure(string errorMessage, string? errorCode = null) =>
        new(false, null, null, null, 0, null, errorMessage, errorCode);
}
