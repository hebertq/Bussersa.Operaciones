using MediatR;
using Modelo.Admin;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Admin.Commands
{
    public record AddLoginUserCommand(UserLogin model) : IRequest<ISingleResponse<UserWithToken>>;
}
