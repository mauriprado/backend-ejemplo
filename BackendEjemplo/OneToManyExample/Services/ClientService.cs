using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.OneToManyExample.Domain.Services;
using BackendEjemplo.OneToManyExample.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;
using BackendEjemplo.Shared.Extensions;
using System.Linq.Expressions;

namespace BackendEjemplo.OneToManyExample.Services
{
    public class ClientService(
        IClientRepository clientRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork) : IClientService
    {
        private static readonly Dictionary<string, Expression<Func<Client, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = c => c.Id,
            ["name"] = c => c.Name,
            ["lastName"] = c => c.LastName,
            ["email"] = c => c.Email,
            ["registrationDate"] = c => c.RegistrationDate
        };

        public async Task<BaseResponse<Client>> AddAsync(Client client, CancellationToken cancellationToken = default)
        {
            await clientRepository.AddAsync(client, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<Client>(client);
        }

        public async Task<BaseResponse<Client>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingClient = await clientRepository.FindByIdAsync(id, cancellationToken);

            if (existingClient is null) return new BaseResponse<Client>($"Cliente con id {id} no existe");

            var clientOrders = await orderRepository.ListPageAsync(0, 1, o => o.ClientId == id, cancellationToken: cancellationToken);

            if (clientOrders.TotalRecords > 0)
                return new BaseResponse<Client>($"No se puede eliminar el cliente con id {id} porque tiene pedidos asociados", isConflict: true);

            clientRepository.Remove(existingClient);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Client>(existingClient);
        }

        public async Task<BaseResponse<Client>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existingClient = await clientRepository.FindByIdAsync(id, cancellationToken);

            if (existingClient is null) return new BaseResponse<Client>($"Cliente con id {id} no existe");

            return new BaseResponse<Client>(existingClient);
        }

        public async Task<Page<Client>> ListPageAsync(ClientPageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<Client, bool>>? filter;

            // Mismo criterio que OrderService.ClientFullName (ver ARCHITECTURE.md sección 7):
            // case-insensitive vía .ToLower() (nunca interpolar $"..." sobre columnas de la
            // entidad, compila a string.Format y Npgsql no lo traduce) y matchea también el
            // nombre completo "Nombre Apellido" combinado, no solo cada campo por separado.
            var fullNameSearchTerm = request.FullName?.ToLower();

            filter = client =>
            (string.IsNullOrWhiteSpace(fullNameSearchTerm) ||
                client.Name.ToLower().Contains(fullNameSearchTerm) ||
                client.LastName.ToLower().Contains(fullNameSearchTerm) ||
                (client.Name.ToLower() + " " + client.LastName.ToLower()).Contains(fullNameSearchTerm)) &&
            (string.IsNullOrWhiteSpace(request.Email) || client.Email.Contains(request.Email)) &&
            (string.IsNullOrWhiteSpace(request.PhoneNumber) || client.PhoneNumber.Contains(request.PhoneNumber)) &&
            (!request.StartRegistrationDate.HasValue || client.RegistrationDate >= request.StartRegistrationDate.Value.ToStartOfBusinessDayUtc()) &&
            (!request.EndRegistrationDate.HasValue || client.RegistrationDate <= request.EndRegistrationDate.Value.ToEndOfBusinessDayUtc());

            var page = await clientRepository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: c => c.Id),
                cancellationToken: cancellationToken);

            return page;
        }

        public async Task<BaseResponse<Client>> UpdateAsync(long id, Client client, CancellationToken cancellationToken = default)
        {
            var existingClient = await clientRepository.FindByIdAsync(id, cancellationToken);

            if (existingClient is null) return new BaseResponse<Client>($"Cliente con id {id} no existe");

            existingClient.Name = client.Name;
            existingClient.LastName = client.LastName;
            existingClient.Email = client.Email;
            existingClient.PhoneNumber = client.PhoneNumber;

            clientRepository.Update(existingClient);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new BaseResponse<Client>(existingClient);
        }
    }
}
