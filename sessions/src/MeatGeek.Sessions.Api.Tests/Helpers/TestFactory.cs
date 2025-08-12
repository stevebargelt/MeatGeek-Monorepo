#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace MeatGeek.Sessions.Api.Tests.Helpers
{
    public static class TestFactory
    {
        public static HttpRequestData CreateHttpRequestData(string? body = null, string method = "GET")
        {
            var functionContext = CreateContext();
            var request = new TestHttpRequestData(functionContext, method, new Uri("http://localhost"));
            
            if (!string.IsNullOrEmpty(body))
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                request.Body.Write(bytes, 0, bytes.Length);
                request.Body.Position = 0; // Reset position to start for reading
            }
            
            return request;
        }

        public static FunctionContext CreateContext()
        {
            var context = new Mock<FunctionContext>();
            
            var services = new ServiceCollection();
            var serviceProviderInstance = services.BuildServiceProvider();
            
            context.Setup(c => c.InstanceServices).Returns(serviceProviderInstance);
            
            return context.Object;
        }
    }

    public class TestHttpRequestData : HttpRequestData
    {
        private readonly FunctionContext _functionContext;
        private readonly string _method;
        private readonly Uri _url;
        private readonly Stream _body;

        public TestHttpRequestData(FunctionContext functionContext, string method, Uri url) : base(functionContext)
        {
            _functionContext = functionContext;
            _method = method;
            _url = url;
            Headers = new HttpHeadersCollection();
            Cookies = new List<IHttpCookie>();
            _body = new MemoryStream();
        }

        public override Stream Body => _body;

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

        public override Uri Url => _url;

        public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();

        public override string Method => _method;

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(_functionContext);
        }
    }

    public class TestHttpResponseData : HttpResponseData
    {
        private readonly HttpCookies _cookies;

        public TestHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
            Headers = new HttpHeadersCollection();
            _cookies = new TestHttpCookies();
            StatusCode = HttpStatusCode.OK;
            Body = new MemoryStream();
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; }

        public override Stream Body { get; set; }

        public override HttpCookies Cookies => _cookies;
    }

    public class TestHttpCookies : HttpCookies
    {
        private readonly List<IHttpCookie> _cookies = new List<IHttpCookie>();

        public override void Append(IHttpCookie cookie)
        {
            _cookies.Add(cookie);
        }

        public override void Append(string name, string value)
        {
            _cookies.Add(new TestHttpCookie(name, value));
        }

        public override IHttpCookie CreateNew()
        {
            return new TestHttpCookie("", "");
        }
    }

    public class TestHttpCookie : IHttpCookie
    {
        public TestHttpCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
        public string? Domain { get; set; }
        public string? Path { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public bool? Secure { get; set; }
        public bool? HttpOnly { get; set; }
        public SameSite SameSite { get; set; } = SameSite.Lax;
        public double? MaxAge { get; set; }
    }

}