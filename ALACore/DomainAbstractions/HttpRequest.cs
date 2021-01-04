using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Handles an HTTP request to a single URL. Supports all HTTP request methods supported by the HttpMethod class. Supports preparing the request on an IEvent and then sending the request on an IDataFlowB&lt;bool&gt; DataChanged event.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Starts the process preparing and sending the HTTP request.</para>
    /// <para>2. IDataFlow&lt;string&gt; fileData: Sets the data to send in the request's PostContent.</para>
    /// <para>3. IDataFlowB&lt;string&gt; url: The URL to send the request to.</para>
    /// <para>4. IDataFlowB&lt;string&gt; jsonData: The sets the Content of the request to be the given JSON string. This replaces the PostContent dictionary.</para>
    /// <para>5. List&lt;IDataFlowB&lt;Tuple&lt;string, string&gt;&gt;&gt; headers: Adds headers to the request.</para>
    /// <para>6. IDataFlowB&lt;string&gt; username: Adds a username field to the PostContent of the request.</para>
    /// <para>7. IDataFlowB&lt;string&gt; email: Adds an email field to the PostContent of the request.</para>
    /// <para>8. IDataFlowB&lt;string&gt; password: Adds a password field to the PostContent of the request.</para>
    /// <para>9. IDataFlowB&lt;string&gt; accessToken: Signs the request with an access token.</para>
    /// <para>10. IDataFlowB&lt;string&gt; refreshToken: Adds a refreshToken field to the PostContent of the request.</para>
    /// <para>11. IDataFlowB&lt;Dictionary&lt;string, string&gt;&gt; postContent: Sets the base PostContent of the request.</para>
    /// <para>12. IDataFlowB&lt;bool&gt; sendRequestFlag: If SendRequestOnFlag is true, then when sendRequestFlag.Data is changed to true, the request is sent.</para>
    /// <para>13. IDataFlow&lt;bool&gt; hasConnection: Outputs whether or not the URL has been successfully connected to. This can be used in combination with "JustPing = true" to use HttpRequest as a simple ping.</para>
    /// <para>14. IDataFlow&lt;string&gt; statusCodeOutput: Outputs the status code, as a string, contained within the HTTP response received after the request has been sent.</para>
    /// <para>15. IDataFlow&lt;Dictionary&lt;string, string&gt;&gt; contentOutput: Outputs the HTTP response string as a Dictionary.</para>
    /// <para>16. IDataFlow&lt;string&gt; responseJsonOutput: Outputs the raw JSON string extracted from the HTTP response.</para>
    /// <para>17. IEvent taskComplete: An event is sent to this destination when the entire process has completed.</para>
    /// </summary>
    public class HttpRequest : IEvent, IDataFlow<string> // start, fileData
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public string URL { get; set; } = "";
        public HttpClient client { get; set; } = new HttpClient();
        public string UserAgent { get; set; } = "";
        public HttpMethod requestMethod { get; set; } = HttpMethod.Post;
        public HttpRequestMessage Request { get; set; }
        public Dictionary<string, string> PostContent { get; set; } = new Dictionary<string, string>();
        public bool JustPing { get; set; } = false;
        public bool SendRequestOnFlag { get; set; } = false;

        // Private fields
        private HttpRequestMessage request;
        private HttpResponseMessage response = default;
        private string responseString = default;
        private string fileData;
        private bool readyToSendRequest = false;

        // Ports
        private IDataFlowB<string> url;
        private IDataFlowB<string> jsonData;
        private List<IDataFlowB<Tuple<string, string>>> headers;
        private IDataFlowB<string> username;
        private IDataFlowB<string> email;
        private IDataFlowB<string> password;
        private IDataFlowB<string> accessToken;
        private IDataFlowB<string> refreshToken;
        private IDataFlowB<Dictionary<string, string>> postContent;
        private IDataFlowB<bool> sendRequestFlag;
        private IDataFlow<bool> hasConnection;
        private IDataFlow<string> statusCodeOutput;
        private IDataFlow<Dictionary<string, string>> contentOutput;
        private IDataFlow<string> responseJsonOutput;
        private IEvent taskComplete;

        /// <summary>
        /// <para>Handles an HTTP request to a single URL. Supports all HTTP request methods supported by the HttpMethod class. Supports preparing the request on an IEvent and then sending the request on an IDataFlowB&lt;bool&gt; DataChanged event.</para>
        /// </summary>
        /// <param name="url"></param>
        public HttpRequest(string url = "")
        {
            this.URL = url;
        }

        private async Task ReadyRequestAsync()
        {

            if (hasConnection != null) // Check internet connection
            {
                try
                {
                    var defaultProxy = WebRequest.DefaultWebProxy;
                    defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                    var tempClient = new WebClient()
                    {
                        Proxy = defaultProxy
                    };
                    tempClient.OpenRead(URL);
                    hasConnection.Data = true;
                }
                catch
                {
                    hasConnection.Data = false;
                }
            }

            if (!JustPing)
            {
                string data = fileData != null ? fileData : "";
                if (url != null) URL = url.Data;
                if (postContent != null) PostContent = postContent.Data;
                if (!string.IsNullOrEmpty(data)) PostContent["Content"] = data;
                request = Request ?? new HttpRequestMessage(requestMethod, URL);
                if (headers != null)
                {
                    foreach (var dataFlowB in headers)
                    {
                        var header = dataFlowB.Data;
                        request.Headers.Add(header.Item1, header.Item2);
                    }
                }
                if (username != null) PostContent["username"] = username.Data;
                if (email != null) PostContent["Email"] = email.Data;
                if (password != null) PostContent["password"] = password.Data;
                if (accessToken != null) request.Headers.Add("Authorization", "Bearer " + accessToken.Data);
                if (refreshToken != null) PostContent["refresh_token"] = refreshToken.Data;

                if (jsonData == null)
                {
                    if (PostContent.Count > 0) request.Content = new FormUrlEncodedContent(PostContent);
                }
                else
                {
                    string json = jsonData.Data;
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    Libraries.Logging.Log("HTTP Request JSON:\n" + json);
                }
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"User-Agent {UserAgent}");
            }

            readyToSendRequest = true;
        }

        private async Task SendRequestAsync()
        {
            if (readyToSendRequest && ((sendRequestFlag == null) || (sendRequestFlag != null && sendRequestFlag.Data == true)))
            {
                if (!JustPing)
                {
                    response = await client.SendAsync(request);
                    responseString = await response.Content.ReadAsStringAsync();
                    Libraries.Logging.Log("HTTP Response JSON:\n" + responseString);
                }
                readyToSendRequest = false;
                if (responseJsonOutput != null) responseJsonOutput.Data = responseString;
                if (statusCodeOutput != null) statusCodeOutput.Data = response.StatusCode.ToString();
                if (contentOutput != null) contentOutput.Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                if (taskComplete != null) taskComplete.Execute();
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Task fireAndForget1 = ReadyRequestAsync();
            Task fireAndForget2 = SendRequestAsync();
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {
                fileData = value;
                Task fireAndForget1 = ReadyRequestAsync();
                Task fireAndForget2 = SendRequestAsync();
            }
        }

        private void PostWiringInitialize()
        {
            if (sendRequestFlag != null)
            {
                sendRequestFlag.DataChanged += () =>
                {
                    if (sendRequestFlag.Data == true)
                    {
                        Task fireAndForget = SendRequestAsync();
                    }
                };
            }
        }
    }
}
