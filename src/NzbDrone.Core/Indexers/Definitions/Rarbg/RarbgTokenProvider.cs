using System;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public interface IRarbgTokenProvider
    {
        string GetToken(RarbgSettings settings);
        void ExpireToken(RarbgSettings settings);
    }

    public class RarbgTokenProvider : IRarbgTokenProvider
    {
        private readonly IIndexerHttpClient _httpClient;
        private readonly ICached<string> _tokenCache;
        private readonly Logger _logger;

        public RarbgTokenProvider(IIndexerHttpClient httpClient, ICacheManager cacheManager, Logger logger)
        {
            _httpClient = httpClient;
            _tokenCache = cacheManager.GetCache<string>(GetType());
            _logger = logger;
        }

        public void ExpireToken(RarbgSettings settings)
        {
            _tokenCache.Remove(settings.BaseUrl);
        }

        public string GetToken(RarbgSettings settings)
        {
            return _tokenCache.Get(settings.BaseUrl,
                () =>
                {
                    var requestBuilder = new HttpRequestBuilder(settings.BaseUrl.Trim('/'))
                        .WithRateLimit(5.0)
                        .Resource($"/pubapi_v2.php?get_token=get_token&app_id={BuildInfo.AppName}")
                        .Accept(HttpAccept.Json);

                    var response = _httpClient.Get<JObject>(requestBuilder.Build());

                    return response.Resource["token"].ToString();
                },
                TimeSpan.FromMinutes(14.0));
        }
    }
}
