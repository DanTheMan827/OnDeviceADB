using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanTheMan827.OnDeviceADB
{
    public class ExitInfo
    {
        public int ExitCode { get; init; }
        public string Output { get; init; }
        public string Error { get; init; }
    }
}
