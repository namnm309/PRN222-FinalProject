using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public class StationDataMergeService : IStationDataMergeService
    {
        public async Task<List<MergedStationDTO>> MergeSerpApiWithDatabaseAsync(
            List<SerpApiPlaceDTO> serpApiPlaces,
            List<ChargingStationDTO> dbStations)
        {
            var mergedStations = new List<MergedStationDTO>();

            foreach (var serpPlace in serpApiPlaces)
            {
                var merged = new MergedStationDTO
                {
                    SerpApiPlaceId = serpPlace.PlaceId,
                    SerpApiTitle = serpPlace.Title,
                    SerpApiAddress = serpPlace.Address,
                    Latitude = (decimal)serpPlace.Latitude,
                    Longitude = (decimal)serpPlace.Longitude,
                    ExternalRating = serpPlace.Rating.HasValue ? (decimal)serpPlace.Rating.Value : null,
                    ExternalReviewCount = serpPlace.Reviews,
                    HasSerpApiData = true,
                    Name = serpPlace.Title,
                    Address = serpPlace.Address
                };

                // Try to match with database station
                var matchedDbStation = FindMatchingDbStation(serpPlace, dbStations);
                if (matchedDbStation != null)
                {
                    merged.Id = matchedDbStation.Id;
                    merged.Name = matchedDbStation.Name;
                    merged.Address = matchedDbStation.Address;
                    merged.City = matchedDbStation.City;
                    merged.Province = matchedDbStation.Province;
                    merged.Status = matchedDbStation.Status;
                    merged.TotalSpots = matchedDbStation.TotalSpots;
                    merged.AvailableSpots = matchedDbStation.AvailableSpots;
                    merged.Phone = matchedDbStation.Phone;
                    merged.Email = matchedDbStation.Email;
                    merged.Description = matchedDbStation.Description;
                    merged.OpeningTime = matchedDbStation.OpeningTime;
                    merged.ClosingTime = matchedDbStation.ClosingTime;
                    merged.Is24Hours = matchedDbStation.Is24Hours;
                    merged.HasDbData = true;

                    // Use DB coordinates if available, otherwise use SerpApi
                    if (matchedDbStation.Latitude.HasValue)
                        merged.Latitude = matchedDbStation.Latitude.Value;
                    if (matchedDbStation.Longitude.HasValue)
                        merged.Longitude = matchedDbStation.Longitude.Value;
                }

                mergedStations.Add(merged);
            }

            // Add DB stations that don't have SerpApi matches
            foreach (var dbStation in dbStations)
            {
                var alreadyMerged = mergedStations.Any(m => m.Id == dbStation.Id);
                if (!alreadyMerged)
                {
                    mergedStations.Add(new MergedStationDTO
                    {
                        Id = dbStation.Id,
                        Name = dbStation.Name,
                        Address = dbStation.Address,
                        City = dbStation.City,
                        Province = dbStation.Province,
                        Latitude = dbStation.Latitude,
                        Longitude = dbStation.Longitude,
                        Status = dbStation.Status,
                        TotalSpots = dbStation.TotalSpots,
                        AvailableSpots = dbStation.AvailableSpots,
                        Phone = dbStation.Phone,
                        Email = dbStation.Email,
                        Description = dbStation.Description,
                        OpeningTime = dbStation.OpeningTime,
                        ClosingTime = dbStation.ClosingTime,
                        Is24Hours = dbStation.Is24Hours,
                        SerpApiPlaceId = dbStation.SerpApiPlaceId,
                        ExternalRating = dbStation.ExternalRating,
                        ExternalReviewCount = dbStation.ExternalReviewCount,
                        HasDbData = true,
                        HasSerpApiData = !string.IsNullOrEmpty(dbStation.SerpApiPlaceId)
                    });
                }
            }

            return await Task.FromResult(mergedStations);
        }

        private ChargingStationDTO? FindMatchingDbStation(SerpApiPlaceDTO serpPlace, List<ChargingStationDTO> dbStations)
        {
            // First try to match by SerpApiPlaceId
            if (!string.IsNullOrEmpty(serpPlace.PlaceId))
            {
                var byPlaceId = dbStations.FirstOrDefault(s => s.SerpApiPlaceId == serpPlace.PlaceId);
                if (byPlaceId != null) return byPlaceId;
            }

            // Then try to match by coordinates (within 100 meters)
            const double thresholdMeters = 0.001; // approximately 100 meters in degrees
            var byCoordinates = dbStations.FirstOrDefault(s =>
                s.Latitude.HasValue && s.Longitude.HasValue &&
                Math.Abs((double)s.Latitude.Value - serpPlace.Latitude) < thresholdMeters &&
                Math.Abs((double)s.Longitude.Value - serpPlace.Longitude) < thresholdMeters);

            if (byCoordinates != null) return byCoordinates;

            // Finally try fuzzy name match
            var byName = dbStations.FirstOrDefault(s =>
                !string.IsNullOrEmpty(s.Name) &&
                (s.Name.Equals(serpPlace.Title, StringComparison.OrdinalIgnoreCase) ||
                 s.Name.Contains(serpPlace.Title, StringComparison.OrdinalIgnoreCase) ||
                 serpPlace.Title.Contains(s.Name, StringComparison.OrdinalIgnoreCase)));

            return byName;
        }
    }
}

