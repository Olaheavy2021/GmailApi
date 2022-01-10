using AutoMapper;
using GmailAPI.ApiHelper;
using GmailAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmailAPI
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Gmail, Mail>();
        }
    }
}
