using System;

namespace ETFPay.Helpers
{
    public static class VrijemeHelper
    {
        private static readonly TimeZoneInfo LokalnaZona = NadjiZonu();

        private static TimeZoneInfo NadjiZonu()
        {
            foreach (var id in new[] { "Europe/Sarajevo", "Central European Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Local;
        }

        public static DateTime ULokalnoVrijeme(this DateTime utc)
        {
            var kaoUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(kaoUtc, LokalnaZona);
        }
    }
}
