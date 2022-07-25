using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.Izipay.Services
{
    public partial class IzipayHttpClient
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IzipayPaymentSettings _izipayPaymentSettings;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IPaymentService _paymentService;
        private readonly ILocalizationService _localizationService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IAddressService _addressService;

        #endregion

        #region Ctor

        public IzipayHttpClient(HttpClient client, ILogger logger,
            ISettingService settingService, IPaymentService paymentService,
            IzipayPaymentSettings izipayPaymentSettings,
            IStoreContext storeContext, IWorkContext workContext,
            ILocalizationService localizationService,
            EmailAccountSettings emailAccountSettings,
            IAddressService addressService,
            IEmailAccountService emailAccountService)
        {
            //configure client
            client.Timeout = TimeSpan.FromSeconds(20);

            _httpClient = client;
            _logger = logger;
            _izipayPaymentSettings = izipayPaymentSettings;
            _settingService = settingService;
            _storeContext = storeContext;
            _workContext = workContext;
            _paymentService = paymentService;
            _localizationService = localizationService;
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _addressService = addressService;
        }

        #endregion

        #region Methods

        public async Task<(string data1, string data2)> CreateTokenAsync(Order order, Customer customer, string productName)
        {
            var url = _izipayPaymentSettings.UrlCreateToken;

            var token64 = string.Format("{0}:{1}", 
                _izipayPaymentSettings.User, 
                _izipayPaymentSettings.UseSandbox ? _izipayPaymentSettings.DevPassword : _izipayPaymentSettings.ProdPassword);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token64.EncodeToBase64());

            var bodyContent = IzipayHelper.ToJsonCreateTokenAsync(order, customer, productName);
            var requestContent = new StringContent(bodyContent, Encoding.UTF8, MimeTypes.ApplicationJson);

            var response = await _httpClient.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions()
            {
                NumberHandling =
                    JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString
            };

            var responsePagoEfectivo = JsonSerializer.Deserialize<ResponseIzipay>(jsonString, jsonOptions);
            return ("","");
        }
        //public async Task<(string xxx)> CreatePaymentAsync()
        //{
        //    return await new Task(null);
        //}

        #endregion


        #region Models

        public class ResponseIzipay
        {
            [JsonPropertyName("webService")]
            public string WebService { get; set; }

            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("applicationVersion")]
            public string ApplicationVersion { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("ticket")]
            public string Ticket { get; set; }

            [JsonPropertyName("serverDate")]
            public string ServerDate { get; set; }

            [JsonPropertyName("applicationProvider")]
            public string ApplicationProvider { get; set; }

            [JsonPropertyName("metadata")]
            public Metadata Metadata { get; set; }

            [JsonPropertyName("mode")]
            public string Mode { get; set; }

            [JsonPropertyName("serverUrl")]
            public string ServerUrl { get; set; }

            [JsonPropertyName("_type")]
            public string Type { get; set; }

            [JsonPropertyName("answer")]
            public Answer Answer { get; set; }
        }

        public class Answer
        {

        }

        public class Metadata
        {

        }

        #endregion
    }
}
