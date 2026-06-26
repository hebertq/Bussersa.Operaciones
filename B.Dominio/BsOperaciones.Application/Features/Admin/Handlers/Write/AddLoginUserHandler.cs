using BsOperaciones.Application.Features.Admin.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Admin.Handlers.Write
{
    public class AddLoginUserHandler : IRequestHandler<AddLoginUserCommand, ISingleResponse<UserWithToken>>
    {
        private readonly IUserService _Hadmin;
        public AddLoginUserHandler(IUserService admin)
        {
            _Hadmin = admin;
        }
        public async Task<ISingleResponse<UserWithToken>> Handle(AddLoginUserCommand request, CancellationToken cancellationToken)
        {
            ISingleResponse<UserWithToken> response = new SingleResponse<UserWithToken>();
            try
            {
                response = await _Hadmin.LoginAsync(request.model);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, "AddLoginUserHandler");
            }

            return await Task.FromResult(response);
        }
    }
}
