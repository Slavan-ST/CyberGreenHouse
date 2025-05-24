using CyberGreenHouse.Models;
using CyberGreenHouse.Models.Response;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Tools
{
    /// <summary>
    /// Класс конвертор для преобразования сырых данных от сервера в модели и обратно
    /// </summary>
    public static class DataConverter
    {
        /// <summary>
        /// Конвертация в модель
        /// </summary>
        /// <typeparam name="T">Type модели</typeparam>
        /// <param name="rawData">Сырые данные от сервера</param>
        /// <returns>Модель T типа</returns>
        /// <exception cref="NotSupportedException">Переданый тип не поддерживается классом</exception>
        public static T Convert<T>(object rawData)
        {
            if (rawData == null)
            {
                return default;
            }
            return typeof(T) switch
            {
                Type sensorType when sensorType == typeof(Sensors) =>
                    (T)(object)ConvertToSensors(rawData as Response),

                Type waterValueType when waterValueType == typeof(WaterValue) =>
                    (T)(object)ConvertToWaterWalue(rawData as Response),

                Type schedulesType when schedulesType == typeof(ObservableCollection<Schedule>) =>
                    (T)(object)ConvertToSchedules(rawData as Response),

                Type lastUpdateType when lastUpdateType == typeof(Plate) =>
                    (T)(object)ConvertToPlate(rawData as Response),

                _ => throw new NotSupportedException($"Unsupported type: {typeof(T)}")
            };
        }

        /// <summary>
        /// Конверт из модели в сырые данные для отправки к серверу
        /// </summary>
        /// <typeparam name="T">Type модели</typeparam>
        /// <param name="model">Модель для обработки</param>
        /// <returns>Сырые данные для отправки</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static object ConvertBack<T>(T model)
        {
            return model switch
            {
                WaterValue waterValue => ConvertWaterValueBack(waterValue),

                ObservableCollection<Schedule> schedules => ConvertSchedulesBack(schedules),

                Plate plate => ConvertPlateBack(plate),

                _ => throw new NotSupportedException($"Unsupported type: {typeof(T)}")
            };
        }



        #region Sensors

        private static object ConvertToSensors(Response response)
        {
            var feed = response?.Feeds?.FirstOrDefault();
            if (feed == null) return default;

            return new Sensors
            {
                Temperature = SafeParseDouble(feed.Field1),
                AirHumidity = SafeParseDouble(feed.Field2),
                SoilHumidity = SafeParseInt(feed.Field3),
                LastUpdate = SafeConvertFromUtc(feed.CreatedAt)
            };
        }

        #endregion

        #region WaterValue

        private static object ConvertToWaterWalue(Response? response)
        {
            var feed = response?.Feeds?.FirstOrDefault();
            if (feed == null) return default;

            return new WaterValue
            {
                State = SafeParseBool(feed.Field1)
            };
        }

        private static string ConvertWaterValueBack(WaterValue waterValue)
        {
            if (waterValue.State)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }

        #endregion

        #region Schedules

        private static object ConvertToSchedules(Response? response)
        {
            var feed = response?.Feeds?.FirstOrDefault();
            if (feed == null) return default;

            return new ObservableCollection<Schedule>
            {
                new Schedule
                {
                    StartTime = SafeParseFromIntSeconds(feed.Field1),
                    EndTime = SafeParseFromIntSeconds(feed.Field1, feed.Field2),
                    IsActive = AreBothNonNegative(feed.Field1, feed.Field2),
                },

                new Schedule
                {
                    StartTime = SafeParseFromIntSeconds(feed.Field3),
                    EndTime = SafeParseFromIntSeconds(feed.Field3, feed.Field4),
                    IsActive = AreBothNonNegative(feed.Field3, feed.Field4),
                },

                new Schedule
                {
                    StartTime = SafeParseFromIntSeconds(feed.Field5),
                    EndTime = SafeParseFromIntSeconds(feed.Field5, feed.Field6),
                    IsActive = AreBothNonNegative(feed.Field5, feed.Field6),
                },

                new Schedule
                {
                    StartTime = SafeParseFromIntSeconds(feed.Field7),
                    EndTime = SafeParseFromIntSeconds(feed.Field7, feed.Field8),
                    IsActive = AreBothNonNegative(feed.Field7, feed.Field8),
                }
            };
        }

        private static object ConvertSchedulesBack(ObservableCollection<Schedule> schedules)
        {
            if (schedules == null)
                return Array.Empty<string>();

            var result = new List<string>();

            foreach (var schedule in schedules)
            {
                if (!schedule.IsActive)
                {
                    result.Add("-1");
                    result.Add("-1");
                }
                else
                {
                    result.Add(GetTotalSecondsAsString(schedule.StartTime) ?? "-1");
                    result.Add(GetTotalSecondsAsString(schedule.StartTime, schedule.EndTime) ?? "-1");
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Plate status

        private static object ConvertToPlate(Response? response)
        {
            var feed = response?.Feeds?.FirstOrDefault();
            if (feed == null) return default;

            return new Plate
            {
                State = SafeParseInt(feed.Field1),
                LastUpdate = SafeConvertFromUtc(feed.CreatedAt)
            };
        }

        private static object ConvertPlateBack(Plate plate)
        {
            return plate.State.ToString();
        }

        #endregion

        // Вспомогательные методы
        private static double SafeParseDouble(string value) =>
            double.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result
            ) ? result : 0.0;

        private static int SafeParseInt(string value) =>
            int.TryParse(value, out var result) ? result : 0;

        private static DateTime SafeConvertFromUtc(DateTime utcTime)
        {
            DateTime fallbackValue = DateTime.Now;
            try
            {
                if (utcTime.Kind == DateTimeKind.Local)
                    return utcTime; // Уже локальное время, преобразование не нужно

                var localTimeZone = TimeZoneInfo.Local;
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return fallbackValue;
            }
            catch (ArgumentException)
            {
                return fallbackValue;
            }
            catch (InvalidTimeZoneException)
            {
                return fallbackValue;
            }
        }

        private static bool SafeParseBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false; // или throw new ArgumentException("Value cannot be empty");

            // Специальные числовые случаи
            if (value == "0") return false;
            if (value == "1") return true;

            // Стандартный парсинг (True/False, true/false, 1/0 и т. д.)
            if (bool.TryParse(value, out bool result))
                return result;

            return false;
        }

        private static TimeSpan SafeParseFromIntSeconds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TimeSpan.Zero;
            }

            if (int.TryParse(value, out int seconds) && seconds >= 0)
            {
                try
                {
                    return TimeSpan.FromSeconds(seconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return TimeSpan.Zero;
                }
            }

            return TimeSpan.Zero;
        }

        private static TimeSpan SafeParseFromIntSeconds(string startTimeInSeconds, string durationInSeconds)
        {
            if (!int.TryParse(startTimeInSeconds, out int startSeconds) || startSeconds < 0)
            {
                return TimeSpan.Zero;
            }

            if (!int.TryParse(durationInSeconds, out int durationSeconds) || durationSeconds < 0)
            {
                durationSeconds = 0;
            }

            try
            {
                TimeSpan startTime = TimeSpan.FromSeconds(startSeconds);
                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                TimeSpan endTime = startTime + duration;

                if (endTime.TotalDays >= 1)
                {
                    endTime = endTime.Add(TimeSpan.FromDays(-(int)endTime.TotalDays));
                }

                return endTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return TimeSpan.Zero;
            }
        }

        private static bool AreBothNonNegative(string firstNumberStr, string secondNumberStr)
        {
            // Пытаемся распарсить первое число
            if (!int.TryParse(firstNumberStr, out int firstNumber))
            {
                // Если не получилось (не число), считаем его отрицательным для безопасности
                return false;
            }

            // Пытаемся распарсить второе число
            if (!int.TryParse(secondNumberStr, out int secondNumber))
            {
                // Если не получилось (не число), считаем его отрицательным для безопасности
                return false;
            }

            // Возвращаем true, только если оба числа >= 0
            return firstNumber >= 0 && secondNumber >= 0;
        }

        private static string GetTotalSecondsAsString(TimeSpan? timeSpan)
        {
            try
            {
                if (!timeSpan.HasValue)
                    return "-1";

                double totalSeconds = timeSpan.Value.TotalSeconds;

                return totalSeconds.ToString();
            }
            catch
            {
                return "-1";
            }
        }

        private static string GetTotalSecondsAsString(TimeSpan? startTime, TimeSpan? endTime)
        {
            try
            {
                if (!startTime.HasValue || !endTime.HasValue)
                    return null;

                TimeSpan difference = endTime.Value - startTime.Value;

                return difference.TotalSeconds.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
