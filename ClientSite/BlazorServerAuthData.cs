using System;

namespace ClientSite
{
    public class BlazorServerAuthData
    {
        public string SubjectId;
        public DateTimeOffset Expiration;
        public string IdToken;
        public string AccessToken;
        public string RefreshToken;
        public DateTimeOffset RefreshAt;
    }
}
