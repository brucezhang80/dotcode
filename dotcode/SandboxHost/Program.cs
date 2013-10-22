using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandboxHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Application is setup by shostmanager, directories, memory limits.
            /*
             Application is copied to it's own directory.
             This process is started with lowpriority and stdio redirected to hostmanager
             Initializes appdomain
             runs code sandboxed.
             exits.
             */

            // Arguments are as follows: dll file name
            // main class
            // entrypoint
            Sandbox sandbox = new Sandbox();
            
            sandbox.ExecuteAssembly()
        }
    }
}
