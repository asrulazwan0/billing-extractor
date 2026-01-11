using AutoMapper;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount != null ? src.TaxAmount.Amount : (decimal?)null))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal != null ? src.Subtotal.Amount : (decimal?)null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<LineItem, LineItemDto>()
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice.Amount))
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.LineTotal.Amount));

        CreateMap<ValidationWarning, ValidationWarningDto>();
        CreateMap<ValidationError, ValidationErrorDto>();

        CreateMap<Invoice, InvoiceSummaryDto>()
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}