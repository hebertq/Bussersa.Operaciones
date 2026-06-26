using MediatR;
using Modelo.Admin;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Admin.Queries
{
    public record GetUserByAccessTokenQuery(RefreshRequest model) : IRequest<ISingleResponse<User>>;
}
