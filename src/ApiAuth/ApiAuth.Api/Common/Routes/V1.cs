namespace ApiAuth.Api.Common.Routes;

internal static class V1
{

    private const string BaseRoute = "api";
    private const string Version = "v1";

    internal static class Users
    {
        public const string SubRoute = $"{BaseRoute}/{Version}/users";

        public const string Register = $"{SubRoute}/register";
    }
}