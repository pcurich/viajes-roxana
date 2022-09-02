using System;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.SafeTyPay.Domain;
using Nop.Plugin.Payments.SafeTyPay.Infrastructure;
using Nop.Plugin.Payments.SafeTyPay.Models;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.SafeTyPay
{
    public static class SafeTyPayExtension
    {
        public static NotificationRequestTemp ToDomain(this NotificationRequest temp)
        {
            return new NotificationRequestTemp()
            {
                Amount = temp.Amount,
                ApiKey = temp.ApiKey,
                RequestDateTime = temp.RequestDateTime,
                MerchantSalesID = temp.MerchantSalesID,
                ReferenceNo = temp.ReferenceNo,
                CreationDateTime = temp.CreationDateTime,
                CurrencyId = temp.CurrencyId,
                PaymentReferenceNo = temp.PaymentReferenceNo,
                StatusCode = temp.StatusCode,
                Signature = temp.Signature
            };
        }

        public static NotifcationResponse ToResponse(this NotificationRequest req, string signatureKey)
        {
            var response = new NotifcationResponse()
            {
                ResponseDateTime = req.RequestDateTime,
                MerchantSalesID = req.MerchantSalesID,
                ReferenceNo = req.ReferenceNo,
                CreationDateTime = req.CreationDateTime,
                Amount = req.Amount,
                CurrencyId = req.CurrencyId,
                PaymentReferenceNo = req.PaymentReferenceNo,
                Status = req.StatusCode,
                OrderNo = req.MerchantSalesID
            };
            response.Signature = SafeTyPayHelper.ComputeSha256Hash(response, signatureKey);
            return response;
        }

        public async static void AddNote(this IOrderService order, string resource, string extra = "")
        {
            await order.InsertOrderNoteAsync(new OrderNote
            {
                CreatedOnUtc = DateTime.UtcNow,
                Note = extra.Length > 0 ? string.Format(resource, extra) : resource
            });
        }
    }
}