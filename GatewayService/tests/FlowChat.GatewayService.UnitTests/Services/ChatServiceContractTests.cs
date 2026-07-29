using System.Text.Json;
using FluentAssertions;
using FlowChat.GatewayService.Api.Services;

namespace FlowChat.GatewayService.UnitTests.Services;

public sealed class ChatServiceContractTests
{
    [Fact]
    public void Deserialize_RangeResponse_PreservesBoundsAndSequence()
    {
        const string json = """
            {
              "items": [
                {
                  "id": "088f5864-9b9d-40bd-8b20-71ca8d70b106",
                  "conversationId": "45564c39-8ac5-4306-a997-541cecb8c716",
                  "senderUserId": "eaa04029-ee26-4337-8985-a97c0a84c235",
                  "text": "hello",
                  "sentAtUtc": "2026-07-27T10:00:00+00:00",
                  "sequenceNum": 121
                }
              ],
              "startSequenceNum": 1,
              "endSequenceNum": 135,
              "currentSequenceNum": 140,
              "hasMore": true
            }
            """;

        var result = JsonSerializer.Deserialize<ConversationMessagesRangeClientDto>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        result.Should().NotBeNull();
        result!.StartSequenceNum.Should().Be(1);
        result.EndSequenceNum.Should().Be(135);
        result.CurrentSequenceNum.Should().Be(140);
        result.Items.Should().ContainSingle()
            .Which.SequenceNum.Should().Be(121);
    }
}
