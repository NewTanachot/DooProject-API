using AutoMapper;
using DooProject.DTO;
using DooProject.Models;

namespace DooProject
{
    public class AutoMapperProfile : Profile
    {
        AutoMapperProfile()
        {
            CreateMap<ProductLookUp, ProdoctDTO>();
            //CreateMap<ProdoctDTO, ProductLookUp>();
        }
    }
}
