using ASC.Business.Interfaces;
using ASC.Model.BaseType;
using ASC.Model.Models;
using ASC.Solution.Configuration;
using ASC.Utilities;
using ASC.Web.Areas.ServiceRequestt.Models;
using ASC.Web.Controllers;
using ASC.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ASC.Web.Areas.ServiceRequests.Controllers
{
    [Area("ServiceRequestt")]
    public class DashboardController : BaseController
    {
        private readonly IServiceRequestOperations _serviceRequestOperations;
        private readonly IMasterDataCacheOperations _masterData;

        public DashboardController(IServiceRequestOperations operations, IMasterDataCacheOperations masterData)
        {
            _serviceRequestOperations = operations;
            _masterData = masterData;
        }
        
        public async Task<IActionResult> Dashboard()
        {
            // List of Status which were to be queried.
            var status = new List<string>
            {
                Status.New.ToString(),
                Status.InProgress.ToString(),
                Status.Initiated.ToString(),
                Status.RequestForInformation.ToString()
            };
                    List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

            if (HttpContext.User.IsInRole(Roles.Admin.ToString()))
            {
                serviceRequests = await _serviceRequestOperations.
                    GetServiceRequestsByRequestedDateAndStatus(DateTime.UtcNow.AddDays(-7), status);
            }
            else if (HttpContext.User.IsInRole(Roles.Engineer.ToString()))
            {
                serviceRequests = await _serviceRequestOperations.GetServiceRequestsByRequestedDateAndStatus(
                    DateTime.UtcNow.AddDays(-7),
                    status,
                    serviceEngineerEmail: HttpContext.User.GetCurrentUserDetails().Email);
            }
            else
            {
                serviceRequests = await _serviceRequestOperations.GetServiceRequestsByRequestedDateAndStatus(
                    DateTime.UtcNow.AddYears(-1),
                    email: HttpContext.User.GetCurrentUserDetails().Email);
            }

            return View(new DashboardViewModel
            {
                ServiceRequests = serviceRequests.OrderByDescending(p => p.RequestedDate).ToList()
            });
        }
    }
}