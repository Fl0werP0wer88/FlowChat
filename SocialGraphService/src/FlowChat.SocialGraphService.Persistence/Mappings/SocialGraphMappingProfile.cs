using AutoMapper;
using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Mappings;

public sealed class SocialGraphMappingProfile : Profile
{
    public SocialGraphMappingProfile()
    {
        CreateMap<ContactEntity, Contact>()
            .ConstructUsing(src => Contact.Create(
                Id<Contact>.FromGuid(src.Id),
                src.UserId1,
                src.UserId2,
                src.IsBlocked,
                src.BlockedBy));

        CreateMap<Contact, ContactEntity>()
            .ConstructUsing(src => ContactEntity.Create(
                src.Id.Value,
                src.UserId1,
                src.UserId2,
                src.IsBlocked,
                src.BlockedBy,
                null,
                src.CreatedBy,
                src.CreatedAtUtc,
                src.LastModifiedBy,
                src.LastModifiedAtUtc));

        CreateMap<InvitationEntity, Invitation>()
            .ConstructUsing(src => new Invitation(
                Id<Invitation>.FromGuid(src.Id),
                src.RequesterId,
                src.AddresseeId,
                ParseInvitationStatus(src.Status),
                src.RespondedAtUtc));

        CreateMap<Invitation, InvitationEntity>()
            .ConstructUsing(src => new InvitationEntity
            {
                Id = src.Id.Value,
                RequesterId = src.RequesterId,
                AddresseeId = src.AddresseeId,
                Status = src.Status.ToString(),
                RespondedAtUtc = src.RespondedAtUtc,
                CreatedBy = src.CreatedBy,
                CreatedAtUtc = src.CreatedAtUtc,
                LastModifiedBy = src.LastModifiedBy,
                LastModifiedAtUtc = src.LastModifiedAtUtc
            });
    }

    private static Domain.Enums.InvitationStatus ParseInvitationStatus(string status)
    {
        if (!Enum.TryParse<Domain.Enums.InvitationStatus>(status, true, out var parsedStatus))
        {
            throw new InvalidOperationException($"Unsupported invitation status '{status}'.");
        }

        return parsedStatus;
    }
}
