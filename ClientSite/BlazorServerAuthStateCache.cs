using System;
using System.Collections.Concurrent;

namespace ClientSite
{
    public class BlazorServerAuthStateCache
    {
        private ConcurrentDictionary<string, BlazorServerAuthData> Cache
            = new ConcurrentDictionary<string, BlazorServerAuthData>();

        public bool HasSubjectId(string subjectId)
            => Cache.ContainsKey(subjectId);

        public void Add(string subjectId, DateTimeOffset expiration, string accessToken, string refreshToken)
        {
            System.Diagnostics.Debug.WriteLine($"Caching sid: {subjectId}");

            var data = new BlazorServerAuthData
            {
                SubjectId = subjectId,
                Expiration = expiration,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            Cache.AddOrUpdate(subjectId, data, (k, v) => data);
        }

        public BlazorServerAuthData Get(string subjectId)
        {
            Cache.TryGetValue(subjectId, out var data);
            return data;
        }

        public void Remove(string subjectId)
        {
            System.Diagnostics.Debug.WriteLine($"Removing sid: {subjectId}");
            Cache.TryRemove(subjectId, out _);
        }
    }
}
