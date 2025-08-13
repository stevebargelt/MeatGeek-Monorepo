using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Azure.Core.Serialization;

namespace MeatGeek.Sessions.Api.Tests
{
    public static class TestFactory
    {
        public static HttpRequestData CreateHttpRequest(string? body = null, string method = "GET", string url = "http://localhost/api/test")
        {
            var context = CreateContext();
            return new TestHttpRequestData(context, method, url, body);
        }

        public static FunctionContext CreateContext()
        {
            var services = new ServiceCollection();
            
            // Configure JSON serializer options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Register the Azure Core JSON object serializer
            var azureSerializer = new JsonObjectSerializer(jsonOptions);
            services.AddSingleton<ObjectSerializer>(azureSerializer);
            
            // Register WorkerOptions with the serializer
            var workerOptions = new WorkerOptions
            {
                Serializer = azureSerializer
            };
            services.AddSingleton<IOptions<WorkerOptions>>(new OptionsWrapper<WorkerOptions>(workerOptions));
            
            var serviceProvider = services.BuildServiceProvider();
            
            var context = new TestFunctionContext();
            context.InstanceServices = serviceProvider;
            
            return context;
        }
    }

    public class TestHttpRequestData : HttpRequestData
    {
        private readonly Stream _body;
        
        public TestHttpRequestData(FunctionContext functionContext, string method = "GET", string url = "http://localhost/api/test", string? body = null) 
            : base(functionContext)
        {
            Method = method;
            Url = new Uri(url);
            Headers = new HttpHeadersCollection();
            _body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? ""));
        }

        public override Stream Body => _body;
        public override HttpHeadersCollection Headers { get; }
        public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();
        public override Uri Url { get; }
        public override IEnumerable<ClaimsIdentity> Identities => new List<ClaimsIdentity>();
        public override string Method { get; }

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }
    }

    public class TestHttpResponseData : HttpResponseData
    {
        private Stream _body = new MemoryStream();
        
        public TestHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode = HttpStatusCode.OK) 
            : base(functionContext)
        {
            StatusCode = statusCode;
            Headers = new HttpHeadersCollection();
            Cookies = new TestHttpCookies();
        }

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; }
        public override Stream Body 
        { 
            get => _body; 
            set => _body = value; 
        }
        public override HttpCookies Cookies { get; }
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
            return new TestHttpCookie();
        }
    }

    public class TestHttpCookie : IHttpCookie
    {
        public TestHttpCookie(string name = "", string value = "")
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public string? Domain { get; set; }
        public string? Path { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public bool? Secure { get; set; }
        public bool? HttpOnly { get; set; }
        public SameSite SameSite { get; set; }
        public double? MaxAge { get; set; }
    }

    public class TestFunctionContext : FunctionContext
    {
        private readonly string _invocationId = Guid.NewGuid().ToString();
        private readonly string _functionId = Guid.NewGuid().ToString();
        private IDictionary<object, object> _items = new Dictionary<object, object>();
        private readonly TestFunctionDefinition _functionDefinition = new TestFunctionDefinition();
        private readonly TestInvocationFeatures _features = new TestInvocationFeatures();
        
        public override string InvocationId => _invocationId;
        public override string FunctionId => _functionId;
        public override TraceContext TraceContext => new TestTraceContext();
        public override BindingContext BindingContext => null!;
        public override RetryContext RetryContext => null!;
        public override IServiceProvider InstanceServices { get; set; } = null!;
        public override FunctionDefinition FunctionDefinition => _functionDefinition;
        public override IDictionary<object, object> Items 
        { 
            get => _items;
            set => _items = value;
        }
        public override IInvocationFeatures Features => _features;
    }

    public class TestTraceContext : TraceContext
    {
        public override string TraceParent => "00-test-01";
        public override string TraceState => "test=true";
    }

    public class TestFunctionDefinition : FunctionDefinition
    {
        public override string Id => "TestFunction";
        public override string Name => "TestFunction";
        public override string EntryPoint => "TestFunction.Run";
        public override string PathToAssembly => "TestAssembly.dll";
        public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
        public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;
    }

    public class TestInvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _features = new Dictionary<Type, object>();
        
        public T Get<T>()
        {
            return _features.TryGetValue(typeof(T), out var feature) ? (T)feature : default(T)!;
        }

        public void Set<T>(T instance)
        {
            if (instance != null)
                _features[typeof(T)] = instance;
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return _features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}