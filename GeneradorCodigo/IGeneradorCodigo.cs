using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProblemSolver.GeneradorCodigo
{
    interface IGeneradorCodigo
    {
        Task<string> GenerarCodigoFuncionAsync(string problema);
        Task<string> GenerarCodigoTestsAsync(string problema, string codigoFuncion);
    }
}
