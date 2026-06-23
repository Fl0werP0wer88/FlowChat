using System.Runtime.CompilerServices;

// Exposes internal domain members to unit tests so invariants can be verified without making them public
[assembly: InternalsVisibleTo("FlowChat.UserProfileService.UnitTests")]
