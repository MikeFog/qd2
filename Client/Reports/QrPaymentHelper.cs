using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using QRCoder;

namespace Merlin.Reports
{
    internal static class QrPaymentHelper
    {
        // Returns a Bitmap of ST00012 payment QR (caller must dispose), or null if agency has no bank details.
        internal static Bitmap GenerateBillQrBitmap(Agency agency, string billNo, int actionId, decimal total)
        {
            string payload = BuildSt00012(agency, billNo, actionId, total);
            if (payload == null)
                return null;

            using (var generator = new QRCodeGenerator())
            {
                QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M, forceUtf8: true);
                using (var code = new QRCode(data))
                    return code.GetGraphic(5);
            }
        }

        private static string BuildSt00012(Agency agency, string billNo, int actionId, decimal total)
        {
            PresentationObject bank = agency.Bank;
            if (bank == null)
                return null;

            string account = agency.Account;
            string bic    = bank[Organization.ParamNames.BankBIK]?.ToString();
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(bic))
                return null;

            string corrAccount = bank[Organization.ParamNames.BankAccount]?.ToString();
            long   sumKopecks  = (long)Math.Round(total * 100, MidpointRounding.AwayFromZero);
            string purpose     = $"Счет №{billNo} к акции {actionId}";

            var sb = new StringBuilder("ST00012");
            Field(sb, "Name",        agency.PrefixWithName);
            Field(sb, "PersonalAcc", account);
            Field(sb, "BankName",    bank.Name);
            Field(sb, "BIC",         bic);
            if (!string.IsNullOrEmpty(corrAccount))
                Field(sb, "CorrespAcc", corrAccount);
            sb.Append($"|Sum={sumKopecks}");
            if (!string.IsNullOrEmpty(agency.INN))
                Field(sb, "PayeeINN", agency.INN);
            Field(sb, "Purpose", purpose);

            return sb.ToString();
        }

        private static void Field(StringBuilder sb, string key, string value)
        {
            sb.Append('|').Append(key).Append('=').Append(value?.Replace("|", " ") ?? string.Empty);
        }

    }
}
