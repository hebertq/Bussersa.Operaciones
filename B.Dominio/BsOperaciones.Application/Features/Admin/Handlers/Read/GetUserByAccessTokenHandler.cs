using BsOperaciones.Application.Features.Admin.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Admin.Handlers.Read
{
    public class GetUserByAccessTokenHandler : IRequestHandler<GetUserByAccessTokenQuery, ISingleResponse<User>>
    {
        private readonly IUserService _Hadmin;
        public GetUserByAccessTokenHandler(IUserService admin)
        {
            _Hadmin = admin;
        }
        public async Task<ISingleResponse<User>> Handle(GetUserByAccessTokenQuery request, CancellationToken cancellationToken)
        {
            ISingleResponse<User> response = new SingleResponse<User>();
            try
            {
                response = await _Hadmin.GetUserByAccessToken(request.model);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, "JobsServices.Publish");
            }

            return await Task.FromResult(response);
        }
    }
}
