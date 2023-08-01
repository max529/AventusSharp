using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AventusSharp.Routes.Response
{
    public interface IResponse
    {
        Task send(HttpContext context);
    }
}
