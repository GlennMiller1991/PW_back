namespace webapi.Services.AuthService;

public class AuthException(string message) : Exception(message);

public class GoogleAuthException() : AuthException("Inappropriate Google account");