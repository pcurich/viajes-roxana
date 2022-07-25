using System.Text.Json;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.Izipay
{
    public static class IzipayHelper
    {
        public static string EncodeToBase64(this string data)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(data);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string ToJsonCreateTokenAsync(Order order, Customer customer, string productName)
        {
            var jo = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonString = JsonSerializer.Serialize(new
            {
                currency = order.CustomerCurrencyCode,
                customer = JsonSerializer.Serialize(new
                {
                    email = customer.Email
                }, jo),
                orderId = order.Id.ToString()
            }, jo);
            ;
            return jsonString;
        }
    }
}
