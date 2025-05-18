using ASC.Model.Models;
using AutoMapper;

namespace ASC.Web.Areas.ServiceRequestt.Models
{
    public class ServiceRequestMappingProfile : Profile
    {
        public ServiceRequestMappingProfile()
        {
            CreateMap<ServiceRequest, NewServiceRequestViewModel>();
            CreateMap<NewServiceRequestViewModel, ServiceRequest>();
        }
    }
}
