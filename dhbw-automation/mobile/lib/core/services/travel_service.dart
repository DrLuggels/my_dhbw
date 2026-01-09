import '../models/travel_models.dart';
import 'api_client.dart';

class TravelService {
  final ApiClient _apiClient;

  TravelService(this._apiClient);

  /// Holt Zugverbindungen basierend auf den Suchkriterien
  Future<TrainConnectionResponse> getConnections(
      TrainConnectionRequest request) async {
    try {
      final response = await _apiClient.post(
        '/api/travel/connections',
        data: request.toJson(),
      );
      return TrainConnectionResponse.fromJson(response.data);
    } catch (e) {
      throw Exception('Fehler beim Abrufen der Verbindungen: $e');
    }
  }

  /// Holt Standard-Route (Laupheim West → Ravensburg)
  Future<TrainConnectionResponse> getDefaultConnections({
    int maxConnections = 5,
  }) async {
    try {
      final response = await _apiClient.get(
        '/api/travel/connections/default',
        queryParameters: {'maxConnections': maxConnections},
      );
      return TrainConnectionResponse.fromJson(response.data);
    } catch (e) {
      throw Exception('Fehler beim Abrufen der Standard-Verbindungen: $e');
    }
  }
}
