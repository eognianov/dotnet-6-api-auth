namespace ApiAuth.Api.Common.Routes;

internal static class V1
{

    private const string BaseRoute = "api";
    private const string Version = "v1";

    internal static class Users
    {
        private const string SubRoute = $"{BaseRoute}/{Version}/users";

        public const string Register = $"{SubRoute}/register";

        public const string Login = $"{SubRoute}/login";

        public const string Refresh = $"{SubRoute}/refresh";
    }
}