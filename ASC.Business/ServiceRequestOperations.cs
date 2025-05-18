using ASC.Business.Interfaces;
using ASC.DataAccess.Interfaces;
using ASC.Model.Models;
using ASC.Model.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASC.Business
{
    public class ServiceRequestOperations : IServiceRequestOperations
    {
        private readonly IUnitOfWork _unitOfWork;

        public ServiceRequestOperations(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateServiceRequestAsync(ServiceRequest request)
        {
            using (_unitOfWork)
            {
                await _unitOfWork.Repository<ServiceRequest>().AddAsync(request);
                _unitOfWork.CommitTransaction();
            }
        
        }
        public ServiceRequest UpdateServiceRequest(ServiceRequest request)
        {
            using (_unitOfWork)
            {
                _unitOfWork.Repository<ServiceRequest>().Update(request);
                _unitOfWork.CommitTransaction();
                return request;
            }
        }

        public async Task<ServiceRequest> UpdateServiceRequestStatusAsync(string rowkey, string partitionkey, string status)
        {
            using (_unitOfWork)
            {
                var serviceRequest = await _unitOfWork.Repository<ServiceRequest>().FindAsync(partitionkey, rowkey);
                if (serviceRequest == null)
                {
                    throw new NullReferenceException();
                }
                serviceRequest.Status = status;
                _unitOfWork.Repository<ServiceRequest>().Update(serviceRequest);
                _unitOfWork.CommitTransaction();
                return serviceRequest;
            }
        }
        public async Task<List<ServiceRequest>> GetServiceRequestsByRequestedDateAndStatus(
            DateTime? requestedDate,
            List<string> status = null,
            string email = "",
            string serviceEngineerEmail = "")
        {
            var query = Queries.GetDashboardQuery(requestedDate, status, email, serviceEngineerEmail);
            var serviceRequests = await _unitOfWork.Repository<ServiceRequest>().FindAllByQuery(query);
            return serviceRequests.ToList();
        }
    }
}
