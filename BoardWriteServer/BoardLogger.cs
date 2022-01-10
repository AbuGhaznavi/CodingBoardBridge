using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardWriteServer
{
    class BoardLogger
    {
        public static void Log(String message)
        {
            Console.WriteLine("BoardWriteServer Log ----- " + message);
        }

    }
}
