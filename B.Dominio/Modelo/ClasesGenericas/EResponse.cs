using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    public enum EResponse
    {
        OK,
        UnexpectedError,
        NoData,
        ValidationError,
        NoPermission,
        UnSuccess,
        InternalServerError,
        BadRequest,
        NotFound,
        Unauthorized
    }
}
