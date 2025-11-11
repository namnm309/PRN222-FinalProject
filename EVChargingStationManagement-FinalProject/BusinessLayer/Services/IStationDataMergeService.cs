using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IStationDataMergeService
    {
        Task<List<MergedStationDTO>> MergeSerpApiWithDatabaseAsync(
            List<SerpApiPlaceDTO> serpApiPlaces,
            List<ChargingStationDTO> dbStations);
    }
}

