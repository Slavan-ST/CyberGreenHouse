using CyberGreenHouse.Models.Response;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CyberGreenHouse.Tools
{
    public class DataService
    {
        private readonly HttpClient _httpClient;

        private readonly string _host = "https://api.thingspeak.com";

        #region API Keys and Chanels
        private readonly string _sensorChannelId = "2405797";
        private readonly string _sensorWriteKey = "PNXZHSN7YKFKMGBW";
        private readonly string _sensorReadKey = "VF7D6JDUS3WKZAO3";

        private readonly string _plateChannelId = "2925691";
        private readonly string _plateWriteKey = "DRJYWUBYD8CW2G8S";
        private readonly string _plateReadKey = "FSAAE1JXYRQPAIDL";

        private readonly string _tapChannelId = "2925696";
        private readonly string _tapWriteKey = "HYJF2C4SZR8BU3X8";
        private readonly string _tapReadKey = "FQ5E6QKU0T0IBRPA";

        private readonly string _scheduleChannelId = "2925698";
        private readonly string _scheduleWriteKey = "B8YP04F9NR5VOX90";
        private readonly string _scheduleReadKey = "Q5EWB0V950NUB9VS";
        #endregion


        public DataService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<DataResult<T>> ExecuteRequestAsync<T>(Func<Task<T>> requestFunc)
        {
            try
            {
                var DataResult = await requestFunc();
                if (DataResult is string strResult && strResult == "-1")
                {
                    throw new ArgumentException("Invalid input data or constants provided.");
                }
                return new DataResult<T> { Data = DataResult };
            }
            catch (ArgumentException)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.ServerError,
                    ErrorMessage = "Ошибка при отправки значения на сервер.\nПроверьте интернет-соединение, если проблема не исчезнет обратитесь в техничекую поддержку"
                };
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx &&
                (socketEx.SocketErrorCode == SocketError.HostNotFound ||
                 socketEx.SocketErrorCode == SocketError.NetworkUnreachable ||
                 socketEx.SocketErrorCode == SocketError.ConnectionRefused))
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.NoInternet,
                    ErrorMessage = "Нет подключения к интернету."
                };
            }
            catch (HttpRequestException ex)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.ServerError,
                    ErrorMessage = $"HTTP ошибка: {ex.Message}\nПроверьте Интернет-соединение."
                };
            }
            catch (TaskCanceledException)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.Timeout,
                    ErrorMessage = "Таймаут запроса."
                };
            }
            catch (JsonException)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.InvalidJson,
                    ErrorMessage = "Ошибка при обработке данных с сервера."
                };
            }
            catch (UriFormatException)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.InvalidUrl,
                    ErrorMessage = "Неверный формат URL."
                };
            }
            catch (Exception ex)
            {
                return new DataResult<T>
                {
                    ErrorType = ErrorTypes.Unknown,
                    ErrorMessage = $"Неизвестная ошибка: {ex.Message}"
                };
            }
        }

        public async Task<DataResult<Response?>> GetSensorData()
        {
            return await ExecuteRequestAsync<Response?>(async () =>
            {
                string _apiUrl = $"{_host}/channels/{_sensorChannelId}/feeds.json?api_key={_sensorReadKey}&results=1";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Response>(json);

                return data;
            });
        }

        public async Task<DataResult<Response?>> GetValueState()
        {
            return await ExecuteRequestAsync<Response?>(async () =>
            {
                string _apiUrl = $"{_host}/channels/{_tapChannelId}/feeds.json?api_key={_tapReadKey}&results=1";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Response>(json);

                return data;
            });
        }

        public async Task<DataResult<string?>> SetWaterValue(string stateWaterVakue)
        {
            return await ExecuteRequestAsync<string?>(async () =>
            {

                string _apiUrl = $"{_host}/update?api_key={_tapWriteKey}&field1={stateWaterVakue}";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var sensorData = JsonConvert.DeserializeObject<string>(json);

                return sensorData;
            });
        }

        public async Task<DataResult<Response?>> GetSchedule()
        {
            return await ExecuteRequestAsync<Response?>(async () =>
            {
                string _apiUrl = $"{_host}/channels/{_scheduleChannelId}/feeds.json?api_key={_scheduleReadKey}&results=1";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Response>(json);

                return data;
            });
        }

        public async Task<DataResult<string?>> SetSchedules(string[] fieldValues)
        {
            return await ExecuteRequestAsync<string?>(async () =>
            {
                var queryBuilder = new StringBuilder($"{_host}/update?api_key={_scheduleWriteKey}");

                for (int i = 0; i < fieldValues.Length; i++)
                {
                    queryBuilder.Append($"&field{i + 1}={fieldValues[i]}");
                }

                string _apiUrl = queryBuilder.ToString();
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<string>(json);

                return data;
            });
        }

        public async Task<DataResult<Response?>> GetPlateStatus()
        {
            return await ExecuteRequestAsync<Response?>(async () =>
            {
                string _apiUrl = $"{_host}/channels/{_plateChannelId}/feeds.json?api_key={_plateReadKey}&results=1";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Response>(json);

                return data;
            });
        }

        public async Task<DataResult<string?>> SetPlateState(string plateState)
        {
            return await ExecuteRequestAsync<string?>(async () =>
            {

                string _apiUrl = $"{_host}/update?api_key={_plateWriteKey}&field1={plateState}";
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var plateStatusEntries = JsonConvert.DeserializeObject<string>(json);

                return plateStatusEntries;
            });
        }
    }
}
