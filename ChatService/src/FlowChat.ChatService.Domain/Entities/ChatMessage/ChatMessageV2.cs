using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed class ChatMessageV2 : AggregateRootBase<ChatMessageV2>
{
    public Id<ConversationV2> ConversationId { get; private set; }
    public Id<UserProfileMarker> SenderUserId { get; private set; }
    public string Text { get; private set; }
    public UtcDateTimeOffset SentAtUtc { get; private set; }
    public UtcDateTimeOffset? DeliveredAtUtc { get; private set; }
    private Guid[] _recipientUserIds = [];
    public IReadOnlyCollection<Id<UserProfileMarker>> RecipientUserIds =>
        [.. _recipientUserIds.Select(Id<UserProfileMarker>.FromGuid)];
    public long? SequenceNum { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; }

    private ChatMessageV2(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        Guid[] recipientUserIds,
        long? sequenceNum = null,
        DeliveryStatus deliveryStatus = DeliveryStatus.Pending,
        UtcDateTimeOffset? deliveredAtUtc = null) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(senderUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (sequenceNum is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNum),
                "Sequence number must be greater than zero.");
        }

        if (!Enum.IsDefined(deliveryStatus))
        {
            throw new ArgumentException("Delivery status is invalid.", nameof(deliveryStatus));
        }

        if (deliveryStatus == DeliveryStatus.Delivered &&
            (!sequenceNum.HasValue || deliveredAtUtc is null))
        {
            throw new InvalidOperationException(
                "Delivered chat message must have a sequence number and delivery timestamp.");
        }

        if (deliveryStatus == DeliveryStatus.Pending && deliveredAtUtc is not null)
        {
            throw new InvalidOperationException(
                "Pending chat message cannot have a delivery timestamp.");
        }

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Text = text.Trim();
        SentAtUtc = sentAtUtc;
        _recipientUserIds = recipientUserIds;
        SequenceNum = sequenceNum;
        DeliveryStatus = deliveryStatus;
        DeliveredAtUtc = deliveredAtUtc;
    }

    public static ChatMessageV2 Create(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        IEnumerable<Id<UserProfileMarker>> recipientUserIds,
        int conversationMembershipRevision,
        IEnumerable<ConversationParticipantUnhideTargetV2>? hiddenParticipants = null,
        UtcDateTimeOffset? sentAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (conversationMembershipRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(conversationMembershipRevision),
                "Conversation membership revision must be greater than zero.");
        }

        var normalizedRecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
        var normalizedHiddenParticipants = NormalizeHiddenParticipants(
            hiddenParticipants,
            normalizedRecipientUserIds);
        var chatMessage = new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc ?? UtcDateTimeOffset.UtcNow,
            normalizedRecipientUserIds);


        //Review1: Pisałeś że publikacja ChatMessageSentIntegrationEvent ma odbywać się przez IAggregateBeforeSaveProcessor. Rezygn z tego podejscia. Niech ChatMessageSentDomainEventV2 ma swój handler który zmapuje ChatMessageSentDomainEventV2 na ChatMessageSentIntegrationEvent i opublikuje.
        //Pozatym wydaje mi się że będzie lepiej jak handler eventu ChatMessageSentDomainEventV2 sam będzie pobierał dane które teraz przychodzą z parametrem conversationMembershipRevision wtedy nie bedzziemy musieli przekazywać go w metodzie Create(). Oceń czy to dobry pomysł?
        chatMessage.AddDomainEvent(new ChatMessageSentDomainEventV2(
            chatMessage.Id,
            chatMessage.ConversationId,
            chatMessage.SenderUserId,
            chatMessage.Text,
            chatMessage.SentAtUtc,
            conversationMembershipRevision));


        //Review1: To mi się nie podoba że każdy z tych eventów w handlerze będzie sobie pobierał i zaisywał ConversationParticipant osobno. 
        //Review1: Lepiej będzie zrobić jeden event, który będzie zawierał listę participantów do odblokowania i w handlerze zrobić 
        //Review1: jedną operację na bazie danych (Może rozszerzyć WriteRepositoryBase?). Mozemy dla tego handlera na razie odejśc od stosowania handlera dziedziczącego z AggregateRootDomainEventHandlerBase.
        //Review1: Pozatym ConversationParticipantUnhideRequestedDomainEventV2 nie brzmi zbyt domentowo Nie lepiej zastosować tu po prostu ChatMessageV2CreatedDomainEvent wywalić parametr hiddenParticipants z Create() oraz klase ConversationParticipantUnhideTargetV2 i po stronie handlera od razu
        //Review1: pobierać agregaty ConversationParticipant które mają IsHidden == True ? Druga opcja jest chyba nawet lepsza?
        foreach (var hiddenParticipant in normalizedHiddenParticipants)
        {
            chatMessage.AddDomainEvent(new ConversationParticipantUnhideRequestedDomainEventV2(
                chatMessage.Id,
                hiddenParticipant.ParticipantId,
                chatMessage.ConversationId,
                hiddenParticipant.UserId));
        }

        return chatMessage;
    }

    public static ChatMessageV2 Restore(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        IEnumerable<Id<UserProfileMarker>> recipientUserIds,
        long? sequenceNum,
        DeliveryStatus deliveryStatus,
        UtcDateTimeOffset? deliveredAtUtc)
    {
        return new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc,
            NormalizeRecipientUserIds(recipientUserIds),
            sequenceNum,
            deliveryStatus,
            deliveredAtUtc);
    }

    public bool SetSequenceNumber(long sequenceNum)
    {
        if (SequenceNum.HasValue)
        {
            return false;
        }

        if (sequenceNum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNum),
                "Sequence number must be greater than zero.");
        }

        SequenceNum = sequenceNum;
        return true;
    }

    public bool MarkAsDelivered(UtcDateTimeOffset deliveredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveredAtUtc);

        if (!SequenceNum.HasValue)
        {
            throw new InvalidOperationException(
                "Sequence number must be set before marking a chat message as delivered.");
        }

        if (DeliveryStatus == DeliveryStatus.Delivered)
        {
            return false;
        }

        DeliveredAtUtc = deliveredAtUtc;
        DeliveryStatus = DeliveryStatus.Delivered;
        return true;
    }

    private static Guid[] NormalizeRecipientUserIds(
        IEnumerable<Id<UserProfileMarker>> recipientUserIds)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        var normalizedRecipientUserIds = recipientUserIds
            .Where(userId => userId is not null)
            .Distinct()
            .Select(userId => userId.Value)
            .ToArray();

        if (normalizedRecipientUserIds.Length == 0)
        {
            throw new InvalidOperationException(
                "Chat message must contain at least one valid recipient.");
        }

        return normalizedRecipientUserIds;
    }

    private static IReadOnlyCollection<ConversationParticipantUnhideTargetV2> NormalizeHiddenParticipants(
        IEnumerable<ConversationParticipantUnhideTargetV2>? hiddenParticipants,
        IReadOnlyCollection<Guid> recipientUserIds)
    {
        var targets = hiddenParticipants?.ToList() ?? [];

        if (targets.Any(target =>
                target is null ||
                target.ParticipantId is null ||
                target.UserId is null))
        {
            throw new ArgumentException(
                "Hidden participant target and its identifiers cannot be null.",
                nameof(hiddenParticipants));
        }

        if (targets.Select(target => target.ParticipantId).Distinct().Count() != targets.Count ||
            targets.Select(target => target.UserId).Distinct().Count() != targets.Count)
        {
            throw new ArgumentException(
                "Hidden participant targets cannot contain duplicates.",
                nameof(hiddenParticipants));
        }

        if (targets.Any(target => !recipientUserIds.Contains(target.UserId.Value)))
        {
            throw new ArgumentException(
                "Only message recipients can be unhidden.",
                nameof(hiddenParticipants));
        }

        return targets;
    }
}
