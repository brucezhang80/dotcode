using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using CompilerService.Fault;
using dotcode.lib.common;
using dotcode.lib.common.Compiler;

namespace CompilerService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ICompilerService
    {
        [OperationContract]
        [FaultContract(typeof(ArgumentNullException))]
        [FaultContract(typeof(UnsupportedLanguageFaultContract))]
        CompilerOutput Compile(CompilerInput compilerInput, string tempCompilerDir);
    }
}
