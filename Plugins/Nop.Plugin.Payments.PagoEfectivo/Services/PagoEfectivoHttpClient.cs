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
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.PagoEfectivo.Services
{
    /// <summary>
    /// Represents the HTTP client to request PayPal services
    /// </summary>
    public partial class PagoEfectivoHttpClient
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly PagoEfectivoPaymentSettings _pagoEfectivoPaymentSettings;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPaymentService _paymentService;
        private readonly ILocalizationService _localizationService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IAddressService _addressService;

        #endregion

        #region Ctor

        public PagoEfectivoHttpClient(HttpClient client,ILogger logger,
            ISettingService settingService, IPaymentService paymentService,
            PagoEfectivoPaymentSettings pagoEfectivoPaymentSettings,
            IStoreContext storeContext, IWorkContext workContext,
            ILocalizationService localizationService,
            IDateTimeHelper dateTimeHelper, 
            EmailAccountSettings emailAccountSettings,
            IAddressService addressService,
            IEmailAccountService emailAccountService)
        {
            //configure client
            client.Timeout = TimeSpan.FromSeconds(20);
            //client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"nopCommerce-{NopVersion.CURRENT_VERSION}");

            _httpClient = client;
            _logger = logger;
            _pagoEfectivoPaymentSettings = pagoEfectivoPaymentSettings;
            _settingService = settingService;
            _storeContext = storeContext;
            _workContext = workContext;
            _paymentService = paymentService;
            _localizationService = localizationService;
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _addressService = addressService;
            _dateTimeHelper = dateTimeHelper; 
        }

        #endregion

        #region Methods

        public async Task<PagoEfectivoPaymentSettings> AuthorizationsAsync()
        {
            var url = _pagoEfectivoPaymentSettings.UseSandbox ?
                string.Format("{0}{1}", _pagoEfectivoPaymentSettings.ApiDev, _pagoEfectivoPaymentSettings.UrlAuthorizationToCip) :
                string.Format("{0}{1}", _pagoEfectivoPaymentSettings.ApiPrd, _pagoEfectivoPaymentSettings.UrlAuthorizationToCip) ;
        
            var bodyContent = ToJSONAuthorizations(_pagoEfectivoPaymentSettings.AccessKey, _pagoEfectivoPaymentSettings.SecretKey, _pagoEfectivoPaymentSettings.IdService.ToString());
            _ = _logger.InsertLogAsync(LogLevel.Information, "AuthorizationsAsync-bodyContent", bodyContent).Result;
            var requestContent = new StringContent(bodyContent,Encoding.UTF8,MimeTypes.ApplicationJson);

            var response = await _httpClient.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions()
            {
                NumberHandling =
                    JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString
            };
            var responseAuthorization = JsonSerializer.Deserialize<ResponsePagoEfectivo>(jsonString, jsonOptions);
            _ = _logger.InsertLogAsync(LogLevel.Information, "AuthorizationsAsync-responseAuthorization", jsonString).Result;

            _pagoEfectivoPaymentSettings.TokenStart = responseAuthorization.Data.TokenStart;
            _pagoEfectivoPaymentSettings.TokenExpires = responseAuthorization.Data.TokenExpires;
            _pagoEfectivoPaymentSettings.CodeService = responseAuthorization.Data.CodeService;
            _pagoEfectivoPaymentSettings.Token = responseAuthorization.Data.Token;

            await _settingService.SaveSettingAsync(_pagoEfectivoPaymentSettings);

           return _pagoEfectivoPaymentSettings;
        }

        public async Task<(string cipUrl, string cip, string TransactionCode, string Code, string Message)> GetCipAsync(Order order , string productName)
        {
            try
            {
                var url = _pagoEfectivoPaymentSettings.UseSandbox ?
                string.Format("{0}{1}", _pagoEfectivoPaymentSettings.ApiDev, _pagoEfectivoPaymentSettings.UrlCip) :
                string.Format("{0}{1}", _pagoEfectivoPaymentSettings.ApiPrd, _pagoEfectivoPaymentSettings.UrlCip);

                _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "es-PE");
                _httpClient.DefaultRequestHeaders.Add(HeaderNames.Origin, "web");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.ApplicationJson));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _pagoEfectivoPaymentSettings.Token);

                var bodyContent = ToJSONCip(order, productName);
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

                var responsePagoEfectivo = JsonSerializer.Deserialize<ResponsePagoEfectivo>(jsonString, jsonOptions);

                switch (responsePagoEfectivo.Code)
                {
                    case 100:
                        return (
                            responsePagoEfectivo.Data.CipUrl,
                            responsePagoEfectivo.Data.Cip.ToString(),
                            responsePagoEfectivo.Data.TransactionCode,
                            responsePagoEfectivo.Code.ToString(),
                            responsePagoEfectivo.Message
                            );
                    default:
                        return (
                            responsePagoEfectivo.Message + " - " + responsePagoEfectivo.Data.Message,
                            responsePagoEfectivo.Data.Cip.ToString(),
                            responsePagoEfectivo.Data.Field,
                            responsePagoEfectivo.Code.ToString() + "-" + responsePagoEfectivo.Data.Code.ToString(),
                            responsePagoEfectivo.Message + " - " + responsePagoEfectivo.Data.Message
                            );
                }
            }
            catch (Exception ex) {
                await _logger.InsertLogAsync(LogLevel.Error, ex.Message, ex.Data.ToString());
                return ("","0","","","");
            }

        }

        public static (string cip, string currency, decimal amount, string paymentDate, string transactionCode) GetCipPayment(string data)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions()
                {
                    NumberHandling =
                    JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString
                };

                var result = JsonSerializer.Deserialize<ResponsePagoEfectivo>(data, jsonOptions);
                return (result.Data.Cip.ToString(), result.Data.Currency, result.Data.Amount, result.Data.PaymentDate, result.Data.TransactionCode);
            }catch(Exception ex)
            { 
                return ("", "", 0, "", "");
            }

        }
        #endregion

        #region Util
        private static string ToJSONAuthorizations(string accessKey, string secretKey, string idService)
        {
            var dateRequest = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var jsonString = JsonSerializer.Serialize(new {
                accessKey = accessKey,
                idService = idService,
                dateRequest = dateRequest,
                hashString = ToSha256Hash(string.Format("{0}.{1}.{2}.{3}", idService, accessKey, secretKey, dateRequest.ToString()))
            });
            return jsonString;
        }

        private string  ToJSONCip(Order order, string productName)
        {
            try
            {
                var store = _storeContext.GetCurrentStore();
                var customer = _workContext.GetCurrentCustomerAsync().Result;
                var address = _addressService.GetAddressByIdAsync(customer.BillingAddressId.HasValue ? customer.BillingAddressId.Value : 0).Result;
                var customXml = _paymentService.DeserializeCustomValues(order);
                var documentType = customXml[_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentType").Result];
                var documentTypeNumber = customXml[_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentTypeValue").Result];
                var phoneNumber = customXml[_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneNumber").Result];
                var phoneCode = customXml[_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneCode").Result];
                var emailStore = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result.Email;
                var jo = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var jsonString = JsonSerializer.Serialize(new
                {
                    currency = order.CustomerCurrencyCode,
                    amount = Math.Round(order.OrderTotal, 2),
                    transactionCode = order.OrderGuid.ToString(),
                    dateExpiry = _pagoEfectivoPaymentSettings.TokenExpires,
                    paymentConcept = productName,
                    additionalData = string.Format("{0}-{1}-{2}", store.Name, store.Id, store.CompanyName),
                    adminEmail = emailStore,
                    userEmail = _workContext.GetCurrentCustomerAsync().Result.Email,
                    userName = address != null ? address.FirstName : "",
                    userLastName = address != null ? address.LastName : "",
                    userDocumentType = documentType,
                    userDocumentNumber = documentTypeNumber,
                    userPhone = phoneNumber,
                    userCodeCountry = phoneCode
                }, jo);
                _ = _logger.InsertLogAsync(LogLevel.Information, "ToJSONCip", jsonString).Result;
                return jsonString;
            }
            catch(Exception ex)
            {
                _ = _logger.InsertLogAsync(LogLevel.Error, ex.Message, ex.Data.ToString()).Result;
                return "";
            }
        }

        private static string ToSha256Hash(string value)
        {
            var sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                var enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (var b in result)
                    sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        #endregion Util

        #region Models
        public class ResponsePagoEfectivo
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }
            [JsonPropertyName("message")]
            public string Message { get; set; }
            [JsonPropertyName("data")]
            public Data Data { get; set; }
        }

        public class Data
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
            [JsonPropertyName("codeService")]
            public string CodeService { get; set; }
            [JsonPropertyName("tokenStart")]
            public string TokenStart { get; set; }
            [JsonPropertyName("tokenExpires")]
            public string TokenExpires { get; set; }
            [JsonPropertyName("cip")]
            public int Cip { get; set; }

            [JsonPropertyName("currency")]
            public string Currency { get; set; }

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }

            [JsonPropertyName("transactionCode")]
            public string TransactionCode { get; set; }

            [JsonPropertyName("dateExpiry")]
            public string DateExpiry { get; set; }

            [JsonPropertyName("paymentDate")]
            public string PaymentDate { get; set; }

            [JsonPropertyName("cipUrl")]
            public string CipUrl { get; set; }

            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("field")]
            public string Field { get; set; }

        }

        #endregion
    }
}
