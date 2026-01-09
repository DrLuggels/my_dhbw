using DHBWAutomation.Backend.API.DTOs;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface ITravelService
{
    Task<TrainConnectionResponse> GetConnectionsAsync(TrainConnectionRequest request);
}
