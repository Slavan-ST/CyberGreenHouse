namespace CyberGreenHouse.Models.Response
{
    public enum ErrorTypes
    {
        None,
        NoInternet,
        ServerError,
        Timeout,
        BadRequest,
        InvalidJson,
        InvalidUrl,
        Unknown
    }
}