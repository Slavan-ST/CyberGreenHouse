using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Models.Response
{
    public class DataResult<T>
    {
        public T? Data { get; set; }
        public ErrorTypes ErrorType { get; set; }
        public string? ErrorMessage { get; set; } = string.Empty;
    }
}
