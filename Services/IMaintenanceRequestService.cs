
using Fixawyy.DTOs;
using Fixawyy.Model_Views;

namespace Fixawyy.Services;

public interface IMaintenanceRequestService
{
    Task<int> CreateRequestAsync(
        CreateMaintenanceRequestDto request,
        string userId);

    Task<List<MaintenanceRequestListDto>> GetMyRequestsAsync(
        string userId);

    Task<MaintenanceRequestDetailsDto?> GetRequestByIdAsync(
        int requestId,
        string userId);

    Task<MaintenanceRequestListPageViewModel> GetRequestsAsync(
        string userId,
        bool canViewAll,
        MaintenanceRequestFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailsPageViewModel?> GetDetailsForUserAsync(
        int requestId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default);

    Task<B2BDashboardViewModel> GetDashboardAsync(
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default);
}
