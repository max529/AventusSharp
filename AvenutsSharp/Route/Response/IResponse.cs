using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AventusSharp.Route.Response
{
    public interface IResponse
    {
        Task send(HttpContext context);
    }
}
