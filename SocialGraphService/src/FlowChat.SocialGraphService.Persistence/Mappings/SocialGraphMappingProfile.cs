using AutoMapper;
using FlowChat.Domain.Abstractions;
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
                src.OwnerUserId,
                src.ContactUserId,
                src.Login,
                src.FirstName,
                src.LastName,
                src.PhoneNumber,
                src.Email,
                src.IsBlocked));

        CreateMap<Contact, ContactEntity>()
            .ConstructUsing(src => ContactEntity.Create(
                src.Id.Value,
                src.OwnerUserId,
                src.ContactUserId,
                src.Login,
                src.FirstName,
                src.LastName,
                src.PhoneNumber,
                src.Email,
                src.IsBlocked,
                null,
                src.CreatedBy,
                src.CreatedAtUtc,
                src.LastModifiedBy,
                src.LastModifiedAtUtc));

        CreateMap<UserSocialGraphEntity, UserSocialGraph>()
            .ConstructUsing(src => new UserSocialGraph(
                Id<UserSocialGraph>.FromGuid(src.Id),
                src.Login,
                src.UserId,
                src.FirstName,
                src.LastName,
                src.PhoneNumber,
                src.Email,
                src.IsPhoneVisible,
                src.IsEmailVisible));

        CreateMap<UserSocialGraph, UserSocialGraphEntity>()
            .ConstructUsing(src => new UserSocialGraphEntity
            {
                Id = src.Id.Value,
                UserId = src.UserId,
                FirstName = src.FirstName,
                LastName = src.LastName,
                Login = src.Login,
                PhoneNumber = src.PhoneNumber,
                Email = src.Email,
                IsPhoneVisible = src.IsPhoneVisible,
                IsEmailVisible = src.IsEmailVisible,
                CreatedBy = src.CreatedBy,
                CreatedAtUtc = src.CreatedAtUtc,
                LastModifiedBy = src.LastModifiedBy,
                LastModifiedAtUtc = src.LastModifiedAtUtc
            });

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
