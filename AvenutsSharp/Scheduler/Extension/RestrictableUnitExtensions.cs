using System;
using AventusSharp.Scheduler.Enum;
using AventusSharp.Scheduler.Unit;
using AventusSharp.Scheduler.Util;

namespace AventusSharp.Scheduler.Extension
{
    /// <summary>
    /// Extension class for restrictable units.
    /// </summary>
    public static class RestrictableUnitExtensions
    {
        /// <summary>
        /// Runs the job between the given start and end hour of day.
        /// </summary>
        /// <param name="unit">The schedule being affected.</param>
        /// <param name="startHour">The start hours (0 through 23).</param>
        /// <param name="startMinute">The start minutes (0 through 59).</param>
        /// <param name="endHour">The end hours (0 through 23).</param>
        /// <param name="endMinute">The end minutes (0 through 59).</param>
        public static ITimeRestrictableUnit Between(this ITimeRestrictableUnit unit, int startHour,
            int startMinute, int endHour, int endMinute)
        {
            if (unit == null)
            {
                throw new ArgumentNullException("unit");
            }

            TimeOfDayRunnableCalculator timeOfDayRunnableCalculator = new TimeOfDayRunnableCalculator(startHour, startMinute, endHour, endMinute);

            Func<DateTime, DateTime>? unboundCalculateNextRun = unit.Schedule.CalculateNextRun;
            unit.Schedule.CalculateNextRun = x =>
            {
                if(unboundCalculateNextRun == null) return DateTime.MaxValue;

                DateTime nextRun = unboundCalculateNextRun(x);

                if (timeOfDayRunnableCalculator.Calculate(nextRun) == TimeOfDayRunnable.TooEarly)
                {
                    nextRun = nextRun.Date.AddHours(startHour).AddMinutes(startMinute);
                }

                while (timeOfDayRunnableCalculator.Calculate(nextRun) != TimeOfDayRunnable.CanRun)
                {
                    nextRun = unboundCalculateNextRun(nextRun);
                }

                return nextRun;
            };

            return unit;
        }

        /// <summary>
        /// Runs the job only on weekdays.
        /// <param name="unit">The schedule being affected</param>
        /// </summary>
        public static IDayRestrictableUnit WeekdaysOnly(this IDayRestrictableUnit unit)
        {
            if (unit == null)
            {
                throw new ArgumentNullException("unit");
            }

            Func<DateTime, DateTime>? unboundCalculateNextRun = unit.Schedule.CalculateNextRun;
            unit.Schedule.CalculateNextRun = x =>
            {
                if(unboundCalculateNextRun == null) return DateTime.MaxValue;

                DateTime nextRun = unboundCalculateNextRun(x);

                while (!nextRun.IsWeekday())
                {
                    nextRun = unit.DayIncrement(nextRun);
                }

                return nextRun;
            };

            return unit;
        }
    }
}