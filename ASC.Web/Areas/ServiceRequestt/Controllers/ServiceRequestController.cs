using ASC.Business.Interfaces;
using ASC.Model.BaseType;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.ServiceRequestt.Models;
using ASC.Web.Controllers;
using ASC.Web.Data;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Web.Areas.ServiceRequestt.Controllers
{
    [Area("ServiceRequestt")]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestOperations _serviceRequestOperations;
        private readonly IMapper _mapper;
        private readonly IMasterDataCacheOperations _masterData;

        public ServiceRequestController(IServiceRequestOperations serviceRequestOperations, IMapper mapper, IMasterDataCacheOperations masterData)
        {
            _serviceRequestOperations = serviceRequestOperations;
            _mapper = mapper;
            _masterData = masterData;
        }

        [HttpGet]
        public async Task<IActionResult> ServiceRequest()
        {
            var masterData = await _masterData.GetMasterDataCacheAsync();
            ViewBag.VehicleTypes = masterData.Values.Where(p => p.PartitionKey == MasterKeys.VehicleType.ToString()).ToList();
            ViewBag.VehicleNames = masterData.Values.Where(p => p.PartitionKey == MasterKeys.VehicleName.ToString()).ToList();
            return View(new NewServiceRequestViewModel());
        }
        [Route("ServiceRequestt/ServiceRequest/ServiceRequest")]
        [HttpPost]
        public async Task<IActionResult> ServiceRequest(NewServiceRequestViewModel request)
        {
            if (ModelState.IsValid)
            {
                var masterData = await _masterData.GetMasterDataCacheAsync();
                ViewBag.VehicleTypes = masterData.Values.Where(p => p.PartitionKey == MasterKeys.VehicleType.ToString()).ToList();
                ViewBag.VehicleNames = masterData.Values.Where(p => p.PartitionKey == MasterKeys.VehicleName.ToString()).ToList();
                return View(request);
            }

            // Map the view model to Azure model
            var serviceRequest = _mapper.Map<NewServiceRequestViewModel, ServiceRequest>(request);
            // Set RowKey, PartitionKey, RequestedDate, Status properties
            serviceRequest.PartitionKey = HttpContext.User.GetCurrentUserDetails().Email;
            serviceRequest.RowKey = Guid.NewGuid().ToString();
            serviceRequest.RequestedDate = request.RequestedDate;
            serviceRequest.Status = Status.New.ToString();
            await _serviceRequestOperations.CreateServiceRequestAsync(serviceRequest);
            return RedirectToAction(/*"Dashboard", "Dashboard", new { Area = "ServiceRequestt" }*/);
        }
    }
}
