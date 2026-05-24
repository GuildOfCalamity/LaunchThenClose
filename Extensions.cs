using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchThenClose
{
    public static class Extensions
    {
        public const double Epsilon = 0.000000000001;

        /// <summary>
        /// Appends the string representation of each element in <paramref name="values"/>,
        /// separated by <paramref name="separator"/>, to the StringBuilder.
        /// Mimics StringBuilder.AppendJoin(IEnumerable&lt;T&gt;) in .NET Core.
        /// </summary>
        public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));
            if (separator == null)
                throw new ArgumentNullException(nameof(separator));
            if (values == null)
                return sb;

            using (var e = values.GetEnumerator())
            {
                if (!e.MoveNext())
                    return sb;  // nothing to append

                // append first element
                sb.Append(e.Current);

                // append remaining with separator
                while (e.MoveNext())
                {
                    sb.Append(separator)
                      .Append(e.Current);
                }
            }

            return sb;
        }

        /// <summary>
        /// Overload that takes a params array.
        /// </summary>
        public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, params T[] values) => sb.AppendJoin(separator, (IEnumerable<T>)values);

        /// <summary>
        /// Overload that takes a single char as separator.
        /// </summary>
        public static StringBuilder AppendJoin<T>(this StringBuilder sb, char separator, IEnumerable<T> values) => sb.AppendJoin(separator.ToString(), values);

        /// <summary>
        /// Converts long file size into typical browser file size.
        /// </summary>
        public static string ToFileSize(this ulong size)
        {
            if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
            if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + "KB"; }
            if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + "MB"; }
            if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + "GB"; }
            if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + "TB"; }
            if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + "PB"; }
            return (size / Math.Pow(1024, 6)).ToString("F0") + "EB";
        }

        /// <summary>
        /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
        /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
        /// </summary>
        /// <param name="span"><see cref="TimeSpan"/></param>
        /// <param name="significantDigits">number of right side digits in output (precision)</param>
        /// <returns></returns>
        public static string ToTimeString(this TimeSpan span, int significantDigits = 3)
        {
            var format = $"G{significantDigits}";
            return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                    : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                    : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                    : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                    : span.TotalDays.ToString(format) + " days")));
        }

        /// <summary>
        /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
        /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
        /// </summary>
        /// <param name="span"><see cref="TimeSpan"/></param>
        /// <param name="significantDigits">number of right side digits in output (precision)</param>
        /// <returns></returns>
        public static string ToTimeString(this TimeSpan? span, int significantDigits = 3)
        {
            var format = $"G{significantDigits}";
            return span?.TotalMilliseconds < 1000 ? span?.TotalMilliseconds.ToString(format) + " milliseconds"
                    : (span?.TotalSeconds < 60 ? span?.TotalSeconds.ToString(format) + " seconds"
                    : (span?.TotalMinutes < 60 ? span?.TotalMinutes.ToString(format) + " minutes"
                    : (span?.TotalHours < 24 ? span?.TotalHours.ToString(format) + " hours"
                    : span?.TotalDays.ToString(format) + " days")));
        }

        /// <summary>
        /// Display a readable sentence as to when the time will happen.
        /// e.g. "in one second" or "in 2 days"
        /// </summary>
        /// <param name="value"><see cref="TimeSpan"/>the future time to compare from now</param>
        /// <returns>human friendly format</returns>
        public static string ToReadableTime(this TimeSpan value, bool reportMilliseconds = false)
        {
            double delta = value.TotalSeconds;
            if (delta < 1 && !reportMilliseconds) { return "less than one second"; }
            if (delta < 1 && reportMilliseconds) { return $"{value.TotalMilliseconds:N1} milliseconds"; }
            if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
            if (delta < 120) { return "a minute"; }                  // 2 * 60
            if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
            if (delta < 5400) { return "an hour"; }                  // 90 * 60
            if (delta < 86400) { return value.Hours + " hours"; }    // 24 * 60 * 60
            if (delta < 172800) { return "one day"; }                // 48 * 60 * 60
            if (delta < 2592000) { return value.Days + " days"; }    // 30 * 24 * 60 * 60
            if (delta < 31104000)                                    // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
                return months <= 1 ? "one month" : months + " months";
            }
            int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
            return years <= 1 ? "one year" : years + " years";
        }

        /// <summary>
        /// Similar to <see cref="GetReadableTime(TimeSpan)"/>.
        /// </summary>
        /// <param name="timeSpan"><see cref="TimeSpan"/></param>
        /// <returns>formatted text</returns>
        public static string ToReadableString(this TimeSpan span)
        {
            var parts = new StringBuilder();
            if (span.Days > 0)
                parts.Append($"{span.Days} day{(span.Days == 1 ? string.Empty : "s")} ");
            if (span.Hours > 0)
                parts.Append($"{span.Hours} hour{(span.Hours == 1 ? string.Empty : "s")} ");
            if (span.Minutes > 0)
                parts.Append($"{span.Minutes} minute{(span.Minutes == 1 ? string.Empty : "s")} ");
            if (span.Seconds > 0)
                parts.Append($"{span.Seconds} second{(span.Seconds == 1 ? string.Empty : "s")} ");
            if (span.Milliseconds > 0)
                parts.Append($"{span.Milliseconds} millisecond{(span.Milliseconds == 1 ? string.Empty : "s")} ");

            if (parts.Length == 0) // result was less than 1 millisecond
                return $"{span.TotalMilliseconds:N4} milliseconds"; // similar to span.Ticks
            else
                return parts.ToString().Trim();
        }

        /// <summary>
        /// Display a readable sentence as to when that time happened.
        /// e.g. "5 minutes ago" or "in 2 days"
        /// </summary>
        /// <param name="value"><see cref="DateTime"/>the past/future time to compare from now</param>
        /// <returns>human friendly format</returns>
        public static string ToReadableTime(this DateTime value, bool useUTC = false)
        {
            TimeSpan ts;
            if (useUTC) { ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks); }
            else { ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks); }

            double delta = ts.TotalSeconds;
            if (delta < 0) // in the future
            {
                delta = Math.Abs(delta);
                if (delta < 1) { return "in less than one second"; }
                if (delta < 60) { return Math.Abs(ts.Seconds) == 1 ? "in one second" : "in " + Math.Abs(ts.Seconds) + " seconds"; }
                if (delta < 120) { return "in a minute"; }
                if (delta < 3000) { return "in " + Math.Abs(ts.Minutes) + " minutes"; } // 50 * 60
                if (delta < 5400) { return "in an hour"; } // 90 * 60
                if (delta < 86400) { return "in " + Math.Abs(ts.Hours) + " hours"; } // 24 * 60 * 60
                if (delta < 172800) { return "tomorrow"; } // 48 * 60 * 60
                if (delta < 2592000) { return "in " + Math.Abs(ts.Days) + " days"; } // 30 * 24 * 60 * 60
                if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
                {
                    int months = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 30));
                    return months <= 1 ? "in one month" : "in " + months + " months";
                }
                int years = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 365));
                return years <= 1 ? "in one year" : "in " + years + " years";
            }
            else // in the past
            {
                if (delta < 1) { return "less than one second ago"; }
                if (delta < 60) { return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago"; }
                if (delta < 120) { return "a minute ago"; }
                if (delta < 3000) { return ts.Minutes + " minutes ago"; } // 50 * 60
                if (delta < 5400) { return "an hour ago"; } // 90 * 60
                if (delta < 86400) { return ts.Hours + " hours ago"; } // 24 * 60 * 60
                if (delta < 172800) { return "yesterday"; } // 48 * 60 * 60
                if (delta < 2592000) { return ts.Days + " days ago"; } // 30 * 24 * 60 * 60
                if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
                {
                    int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    return months <= 1 ? "one month ago" : months + " months ago";
                }
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        /// <summary>
        /// Display a readable sentence as to when the time will happen.
        /// e.g. "8 minutes 0 milliseconds"
        /// </summary>
        /// <param name="milliseconds">integer value</param>
        /// <returns>human friendly format</returns>
        public static string ToReadableTime(int milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentException("Milliseconds cannot be negative.");

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            if (timeSpan.TotalHours >= 1)
            {
                return string.Format("{0:0} hour{1} {2:0} minute{3}",
                    timeSpan.Hours, timeSpan.Hours == 1 ? "" : "s",
                    timeSpan.Minutes, timeSpan.Minutes == 1 ? "" : "s");
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return string.Format("{0:0} minute{1} {2:0} second{3}",
                    timeSpan.Minutes, timeSpan.Minutes == 1 ? "" : "s",
                    timeSpan.Seconds, timeSpan.Seconds == 1 ? "" : "s");
            }
            else
            {
                return string.Format("{0:0} second{1} {2:0} millisecond{3}",
                    timeSpan.Seconds, timeSpan.Seconds == 1 ? "" : "s",
                    timeSpan.Milliseconds, timeSpan.Milliseconds == 1 ? "" : "s");
            }
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> into a string of the format "hh:mm:ss" or "dd:hh:mm:ss" if days are present.
        /// </summary>
        /// <param name="ts">The TimeSpan to format.</param>
        /// <returns>A string representing the TimeSpan in a human-readable format.</returns>
        public static string ToHoursMinutesSeconds(this TimeSpan ts) => ts.Days > 0 ? (ts.Days * 24 + ts.Hours) + ts.ToString("':'mm':'ss") : ts.ToString("hh':'mm':'ss");

        /// <summary>
        /// Converts a time specified by hour, minute, and second to 100-nanosecond ticks.
        /// </summary>
        /// <remarks>Computes total seconds as hour * 3600 + minute * 60 + second, multiplies by
        /// TimeSpan.TicksPerSecond, and verifies the result fits within Int64.</remarks>
        /// <param name="hour">The number of hours.</param>
        /// <param name="minute">The number of minutes.</param>
        /// <param name="second">The number of seconds.</param>
        /// <returns>The total number of 100-nanosecond ticks that represent the specified time.</returns>
        /// <exception cref="Exception">Thrown when the computed time in ticks is outside the range that can be represented by a 64-bit signed integer.</exception>
        public static long TimeToTicks(int hour, int minute, int second)
        {
            long MaxSeconds = long.MaxValue / 10000000; // => MaxValue / TimeSpan.TicksPerSecond
            long MinSeconds = long.MinValue / 10000000; // => MinValue / TimeSpan.TicksPerSecond

            // "totalSeconds" is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            long totalSeconds = (long)hour * 3600 + (long)minute * 60 + (long)second;

            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new Exception("Argument out of range: TimeSpan too long.");

            return totalSeconds * 10000000; // => totalSeconds * TimeSpan.TicksPerSecond
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> into a human-friendly readable string.
        /// </summary>
        /// <param name="timeSpan"><see cref="TimeSpan"/> to convert (can be negative)</param>
        /// <returns>human-friendly string representation of the given <see cref="TimeSpan"/></returns>
        public static string ToHumanFriendlyString(this TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
                return "0 seconds";

            bool isNegative = false;
            List<string> parts = new List<string>();

            // Check for negative TimeSpan.
            if (timeSpan < TimeSpan.Zero)
            {
                isNegative = true;
                timeSpan = timeSpan.Negate(); // Make it positive for the calculations.
            }

            if (timeSpan.Days > 0)
                parts.Add($"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")}");
            if (timeSpan.Hours > 0)
                parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")}");
            if (timeSpan.Minutes > 0)
                parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}");
            if (timeSpan.Seconds > 0)
                parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}");

            // If no large amounts so far, try milliseconds.
            if (parts.Count == 0 && timeSpan.Milliseconds > 0)
                parts.Add($"{timeSpan.Milliseconds} millisecond{(timeSpan.Milliseconds > 1 ? "s" : "")}");

            // If no milliseconds, use ticks (nanoseconds).
            if (parts.Count == 0 && timeSpan.Ticks > 0)
            {
                // A tick is equal to 100 nanoseconds. While this maps well into units of time
                // such as hours and days, any periods longer than that aren't representable in
                // a succinct fashion, e.g. a month can be between 28 and 31 days, while a year
                // can contain 365 or 366 days. A decade can have between 1 and 3 leap-years,
                // depending on when you map the TimeSpan into the calendar. This is why TimeSpan
                // does not provide a "Years" property or a "Months" property.
                // Internally TimeSpan uses long (Int64) for its values, so:
                //  - TimeSpan.MaxValue = long.MaxValue
                //  - TimeSpan.MinValue = long.MinValue
                //  - TimeSpan.TicksPerMicrosecond = 10 (not available in older .NET versions)
                parts.Add($"{(timeSpan.Ticks * 10)} microsecond{((timeSpan.Ticks * 10) > 1 ? "s" : "")}");
            }

            // Join the sections with commas & "and" for the last one.
            if (parts.Count == 1)
                return isNegative ? $"Negative {parts[0]}" : parts[0];
            else if (parts.Count == 2)
                return isNegative ? $"Negative {string.Join(" and ", parts)}" : string.Join(" and ", parts);
            else
            {
                string lastPart = parts[parts.Count - 1];
                parts.RemoveAt(parts.Count - 1);
                return isNegative ? $"Negative " + string.Join(", ", parts) + " and " + lastPart : string.Join(", ", parts) + " and " + lastPart;
            }
        }

        /// <summary>
        /// uint max = 4,294,967,295 (4.29 Gbps)
        /// </summary>
        /// <returns>formatted bit-rate string</returns>
        public static string FormatBitrate(this uint amount)
        {
            var sizes = new string[]
            {
                "bps",
                "Kbps", // kilo
                "Mbps", // mega
                "Gbps", // giga
                "Tbps", // tera
            };
            var order = amount.OrderOfMagnitude();
            var speed = amount / Math.Pow(1000, order);
            return $"{speed:0.##} {sizes[order]}";
        }

        /// <summary>
        /// ulong max = 18,446,744,073,709,551,615 (18.45 Ebps)
        /// </summary>
        /// <returns>formatted bit-rate string</returns>
        public static string FormatBitrate(this ulong amount)
        {
            var sizes = new string[]
            {
                "bps",
                "Kbps", // kilo
                "Mbps", // mega
                "Gbps", // giga
                "Tbps", // tera
                "Pbps", // peta
                "Ebps", // exa
                "Zbps", // zetta
                "Ybps"  // yotta
            };
            var order = amount.OrderOfMagnitude();
            var speed = amount / Math.Pow(1000, order);
            return $"{speed:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Returns the order of magnitude (10^3)
        /// </summary>
        public static int OrderOfMagnitude(this ulong amount) => (int)Math.Floor(Math.Log(amount, 1000));

        /// <summary>
        /// Returns the order of magnitude (10^3)
        /// </summary>
        public static int OrderOfMagnitude(this uint amount) => (int)Math.Floor(Math.Log(amount, 1000));

        /// <summary>
        /// Checks to see if a date is between <paramref name="begin"/> and <paramref name="end"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="dt"/> is between <paramref name="begin"/> and <paramref name="end"/>, otherwise <c>false</c>
        /// </returns>
        public static bool IsBetween(this DateTime dt, DateTime begin, DateTime end) => dt.Ticks >= begin.Ticks && dt.Ticks <= end.Ticks;

        /// <summary>
        /// Determine if the current time is between two <see cref="TimeSpan"/>s.
        /// </summary>
        /// <param name="ts">DateTime.Now.TimeOfDay</param>
        /// <param name="start">TimeSpan.Parse("23:00:00")</param>
        /// <param name="end">TimeSpan.Parse("02:30:00")</param>
        /// <returns><c>true</c> if between start and end, <c>false</c> otherwise</returns>
        public static bool IsBetween(this TimeSpan ts, TimeSpan start, TimeSpan end)
        {
            // Are we in the same day.
            if (start <= end)
                return ts >= start && ts <= end;

            // Are we on different days.
            return ts >= start || ts <= end;
        }

        /// <summary>
        /// Compares the current <see cref="DateTime.Now.TimeOfDay"/> to the 
        /// given <paramref name="start"/> and <paramref name="end"/> times.
        /// </summary>
        /// <returns><c>true</c> if between start and end, <c>false</c> otherwise</returns>
        public static bool IsNowBetween(string start = "10:00:00", string end = "14:00:00")
        {
            try
            {
                var tsNow = DateTime.Now.TimeOfDay;
                var tsStart = TimeSpan.Parse(start);
                var tsEnd = TimeSpan.Parse(end);
                if (tsStart <= tsEnd)
                    return tsNow >= tsStart && tsNow <= tsEnd;

                return tsNow >= tsStart || tsNow <= tsEnd;
            }
            catch (Exception ex) { Debug.WriteLine($"[ERROR] IsNowBetween: {ex.Message}"); }
            return false;
        }

        /// <summary>
        /// Compares two <see cref="DateTime"/>s ignoring the hours, minutes and seconds.
        /// </summary>
        public static bool AreDatesSimilar(this DateTime? date1, DateTime? date2)
        {
            if (date1 is null && date2 is null)
                return true;

            if (date1 is null || date2 is null)
                return false;

            return date1.Value.Year == date2.Value.Year &&
                   date1.Value.Month == date2.Value.Month &&
                   date1.Value.Day == date2.Value.Day;
        }

        /// <summary>
        /// Returns the start of the day (midnight) for a given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime"><see cref="DateTime"/></param>
        /// <returns>A new DateTime representing the start of the day</returns>
        public static DateTime StartOfDay(this DateTime dateTime) => dateTime.Date; // or new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);

        /// <summary>
        /// Returns the end of the day (23:59:59.999) for a given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime"><see cref="DateTime"/></param>
        /// <returns>A new DateTime representing the end of the day</returns>
        public static DateTime EndOfDay(this DateTime dateTime) => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999);

        /// <summary>
        /// Returns a range of <see cref="DateTime"/> objects matching the criteria provided.
        /// </summary>
        /// <example>
        /// IEnumerable<DateTime> dateRange = DateTime.Now.GetDateRangeTo(DateTime.Now.AddDays(80));
        /// </example>
        /// <param name="self"><see cref="DateTime"/></param>
        /// <param name="toDate"><see cref="DateTime"/></param>
        /// <returns><see cref="IEnumerable{DateTime}"/></returns>
        public static IEnumerable<DateTime> GetDateRangeTo(this DateTime self, DateTime toDate)
        {
            // Query Syntax:
            //IEnumerable<int> range = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days);
            //IEnumerable<DateTime> dates = from p in range select self.Date.AddDays(p);

            // Method Syntax:
            IEnumerable<DateTime> dates = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days).Select(p => self.Date.AddDays(p));

            return dates;
        }

        /// <summary>
        /// Returns an inclusive sequence of <see cref="TimeSpan"/>s from <paramref name="start"/> 
        /// to <paramref name="end"/>, stepping by <paramref name="step"/> each iteration.
        /// </summary>
        /// <param name="start">The first <see cref="TimeSpan"/> in the sequence.</param>
        /// <param name="end">The last <see cref="TimeSpan"/> in the sequence (inclusive).</param>
        /// <param name="step">The increment between consecutive <see cref="TimeSpan"/>s.</param>
        /// <returns><see cref="IEnumerable{T}"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="step"/> is zero or negative,
        /// or if <paramref name="end"/> is earlier than <paramref name="start"/>.
        /// </exception>
        public static IEnumerable<TimeSpan> Range(TimeSpan start, TimeSpan end, TimeSpan step)
        {
            if (step <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(step), "Step must be positive.");
            if (end < start)
                throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to start.");

            // Calculate how many steps will fit (inclusive)
            long totalTicks = end.Ticks - start.Ticks;
            long stepTicks = step.Ticks;
            int stepCount = (int)(totalTicks / stepTicks) + 1;

            return Enumerable.Range(0, stepCount).Select(i => TimeSpan.FromTicks(start.Ticks + i * stepTicks));
        }

        /// <summary>
        /// Returns an inclusive sequence of <see cref="TimeSpan"/>s from <paramref name="start"/> 
        /// to <paramref name="end"/>, stepping by 1 tick each iteration.
        /// </summary>
        /// <param name="start">The first <see cref="TimeSpan"/> in the sequence.</param>
        /// <param name="end">The last <see cref="TimeSpan"/> in the sequence (inclusive).</param>
        /// <returns><see cref="IEnumerable{T}"/></returns>
        public static IEnumerable<TimeSpan> Range(TimeSpan start, TimeSpan end)
        {
            return Range(start, end, TimeSpan.FromTicks(1));
        }

        /// <summary>
        /// Returns an <see cref="Int32"/> amount of days between two <see cref="DateTime"/> objects.
        /// </summary>
        /// <param name="self"><see cref="DateTime"/></param>
        /// <param name="toDate"><see cref="DateTime"/></param>
        public static int GetDaysBetween(this DateTime self, DateTime toDate) => new TimeSpan(toDate.Ticks - self.Ticks).Days;

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> amount between two <see cref="DateTime"/> objects.
        /// </summary>
        /// <param name="self"><see cref="DateTime"/></param>
        /// <param name="toDate"><see cref="DateTime"/></param>
        public static TimeSpan GetTimeSpanBetween(this DateTime self, DateTime toDate) => new TimeSpan(toDate.Ticks - self.Ticks);

        /// <summary>
        /// Determines if the given <paramref name="dateTime"/> is older than <paramref name="days"/>.
        /// </summary>
        /// <returns><c>true</c> if older, <c>false</c> otherwise</returns>
        public static bool IsOlderThanDays(this DateTime dateTime, double days = 1.0)
        {
            if (days.IsInvalidOrZero())
                throw new ArgumentOutOfRangeException(nameof(days), "Days cannot be zero or negative.");

            TimeSpan timeDifference = DateTime.Now - dateTime;
            return timeDifference.TotalDays > days;
        }

        /// <summary>
        /// Determine the Next date by passing in a DayOfWeek (i.e. from this date, when is the next Tuesday?)
        /// </summary>
        public static DateTime Next(this DateTime current, DayOfWeek dayOfWeek)
        {
            int offsetDays = dayOfWeek - current.DayOfWeek;
            if (offsetDays <= 0)
            {
                offsetDays += 7;
            }
            DateTime result = current.AddDays(offsetDays);
            return result;
        }

        /// <summary>
        /// Converts a DateTime to a DateTimeOffset with the specified offset
        /// </summary>
        /// <param name="date">The DateTime to convert</param>
        /// <param name="offset">The offset to apply to the date field</param>
        /// <returns>The corresponding DateTimeOffset</returns>
        public static DateTimeOffset ToOffset(this DateTime date, TimeSpan offset) => new DateTimeOffset(date).ToOffset(offset);

        /// <summary>
        /// Accounts for once the <paramref name="date1"/> is past <paramref name="date2"/>
        /// or falls within the amount of <paramref name="days"/>.
        /// </summary>
        public static bool WithinDaysOrPast(this DateTime date1, DateTime date2, double days = 7.0)
        {
            if (date1 > date2) // Account for past-due amounts.
                return true;
            else
            {
                TimeSpan difference = date1 - date2;
                return Math.Abs(difference.TotalDays) <= days;
            }
        }

        /// <summary>
        /// Multiplies the given <see cref="TimeSpan"/> by the scalar amount provided.
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan timeSpan, double scalar) => new TimeSpan((long)(timeSpan.Ticks * scalar));

        public static bool HasAlpha(this string str)
        {
            if (string.IsNullOrEmpty(str)) { return false; }
            return str.Any(x => char.IsLetter(x));
        }

        public static bool HasNumeric(this string str)
        {
            if (string.IsNullOrEmpty(str)) { return false; }
            return str.Any(x => char.IsNumber(x));
        }

        public static bool HasSpace(this string str)
        {
            if (string.IsNullOrEmpty(str)) { return false; }
            return str.Any(x => char.IsSeparator(x));
        }

        public static bool HasPunctuation(this string str)
        {
            if (string.IsNullOrEmpty(str)) { return false; }
            return str.Any(x => char.IsPunctuation(x));
        }

        /// <summary>
        /// Consider anything within an order of magnitude of epsilon to be zero.
        /// </summary>
        /// <param name="value">The <see cref="double"/> to check</param>
        /// <returns>
        /// True if the number is zero, false otherwise.
        /// </returns>
        public static bool IsZero(this double value) => Math.Abs(value) < Epsilon;

        public static bool IsInvalid(this double value)
        {
            if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity)
                return true;

            return false;
        }

        /// <summary>
        /// We'll consider zero anything less than or equal to zero.
        /// </summary>
        public static bool IsInvalidOrZero(this double value)
        {
            if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity || value <= 0)
                return true;

            return false;
        }

        public static bool IsOne(this double value)
        {
            return Math.Abs(value) >= 1d - Epsilon && Math.Abs(value) <= 1d + Epsilon;
        }

        /// <summary>
        /// Tries to execute the given <paramref name="action"/> for a maximum of 
        /// <paramref name="max"/> time stepping by 1 additional second each iteration.
        /// </summary>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise</returns>
        public static bool TryForThisLongOrUntilSuccessful(Action action, TimeSpan max)
        {
            if (max <= TimeSpan.FromSeconds(1))
                max = TimeSpan.FromSeconds(2);

            bool success = false;

            foreach (var ts in Extensions.Range(TimeSpan.FromSeconds(1), max, TimeSpan.FromSeconds(1)))
            {
                try
                {
                    action();
                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    Console.WriteLine($"Trying again in {ts.ToReadableString()}…");
                    Thread.Sleep(ts);
                }

                if (success)
                    break; // Exit the loop if action was successful
            }

            return success;
        }

        /// <summary>
        /// Tries to execute the given <paramref name="action"/> for a maximum of
        /// <paramref name="max"/> time, stepping by 1 additional second each iteration.
        /// </summary>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static async Task<bool> TryForThisLongOrUntilSuccessfulAsync(Action action, TimeSpan max)
        {
            if (max <= TimeSpan.FromSeconds(1))
                max = TimeSpan.FromSeconds(2);

            bool success = false;

            foreach (var ts in Extensions.Range(TimeSpan.FromSeconds(1), max, TimeSpan.FromSeconds(1)))
            {
                try
                {
                    action();
                    success = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] {ex.Message}");
                    Debug.WriteLine($"Trying again in {ts.ToReadableString()}…");

                    await Task.Delay(ts).ConfigureAwait(false);
                }

                if (success)
                    break;
            }

            return success;
        }

        /// <summary>
        /// Schedules the given action to run once after the specified delay.
        /// This is fire-and-forget: the action runs on a ThreadPool thread.
        /// </summary>
        /// <param name="action">The callback to invoke.</param>
        /// <param name="delay">How long to wait before invoking.</param>
        /// <exception cref="ArgumentNullException">If action is null.</exception>
        public static void ExecuteAfter(Action action, TimeSpan delay, Action<Exception> onError = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // We need to capture the timer so we can dispose it after firing
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer(_ =>
            {
                // Clean up the timer to avoid leaks
                timer.Dispose();
                try { action(); }
                catch (Exception ex) { onError?.Invoke(ex); }
            },
            state: null,
            dueTime: delay,
            period: System.Threading.Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Asynchronously waits for the delay, then invokes the action.
        /// Exceptions thrown by the action will fault the returned Task.
        /// </summary>
        /// <param name="action">The callback to invoke.</param>
        /// <param name="delay">How long to wait before invoking.</param>
        /// <returns>A Task that completes once the action has run.</returns>
        /// <exception cref="ArgumentNullException">If action is null.</exception>
        public static async Task ExecuteAfterAsync(Action action, TimeSpan delay, CancellationToken token = default, Action<Exception> onError = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                // We don't care about the code below the Task continuing on the
                // original SynchronizationContext, so we'll use ConfigureAwait(false)
                // to save some thread syncing time (a small gain).
                await Task.Delay(delay, token).ConfigureAwait(false);
                action();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                //throw; // Re-throw to allow caller to handle
            }
        }

        /// <summary>
        /// Executes an action on a new thread using the <see cref="ThreadPool"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to perform.</param>
        public static void RunThreaded(Action action)
        {
            ThreadPool.QueueUserWorkItem(_ => {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] RunThreaded: {ex.Message}");
                }
            });
        }
    }
}
