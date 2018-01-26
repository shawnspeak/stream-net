using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Stream.Rest
{
    internal class RestClient : IDisposable
    {
        readonly Uri _baseUrl;
        private TimeSpan _timeout;
        private HttpClient _httpClient;

        public RestClient(Uri baseUrl, TimeSpan timeout)
        {
            _baseUrl = baseUrl;
            _timeout = timeout;

            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = _timeout;
        }     

        private HttpClient GetClient()
        {
#if NET45
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
            return _httpClient;
        }

        private Uri BuildUri(RestRequest request)
        {
            var queryString = "";
            request.QueryParameters.ForEach((p) =>
            {
                queryString += (queryString.Length == 0) ? "?" : "&";
                queryString += string.Format("{0}={1}", p.Key, Uri.EscapeDataString(p.Value.ToString()));
            });
            return new Uri(_baseUrl, request.Resource + queryString);
        }

        public async Task<RestResponse> Execute(RestRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request", "Request is required");

            var client = GetClient();

            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = BuildUri(request)
            };

            // setup method and content if needed
            switch (request.Method)
            {
                case HttpMethod.DELETE:
                    httpRequest.Method = System.Net.Http.HttpMethod.Delete;
                    break;
                case HttpMethod.POST:                    
                    httpRequest.Method = System.Net.Http.HttpMethod.Post;
                    httpRequest.Content = new StringContent(request.JsonBody, Encoding.UTF8, "application/json");
                    break;                    
                default:
                    httpRequest.Method = System.Net.Http.HttpMethod.Get;
                    break;
            }

            // add request headers
            httpRequest.Headers.Clear();
            request.Headers.ForEach(h =>
            {
                httpRequest.Headers.Add(h.Key, h.Value);
            });

            HttpResponseMessage response = await client.SendAsync(httpRequest);
            return await RestResponse.FromResponseMessage(response);
        }              

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
            }
        }
    }
}
