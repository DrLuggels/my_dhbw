import 'package:dio/dio.dart';
import '../config/api_config.dart';
import '../models/api_response.dart';
import '../models/calendar_event.dart';
import 'api_client.dart';

class CalendarService {
  final ApiClient _apiClient;

  CalendarService(this._apiClient);

  Future<List<CalendarEvent>> getEvents({
    DateTime? startDate,
    DateTime? endDate,
    String? source,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (startDate != null) queryParams['startDate'] = startDate.toIso8601String();
      if (endDate != null) queryParams['endDate'] = endDate.toIso8601String();
      if (source != null) queryParams['source'] = source;

      final response = await _apiClient.get(
        ApiConfig.calendar,
        queryParameters: queryParams,
      );

      final apiResponse = ApiResponse<List<dynamic>>.fromJson(
        response.data,
        (json) => json as List<dynamic>,
      );

      if (!apiResponse.success || apiResponse.data == null) {
        return [];
      }

      return apiResponse.data!
          .map((e) => CalendarEvent.fromJson(e))
          .toList();
    } catch (e) {
      print('CalendarService.getEvents error: $e');
      return [];
    }
  }

  Future<void> syncRapla() async {
    try {
      await _apiClient.post('${ApiConfig.calendar}/rapla/sync');
    } catch (e) {
      print('CalendarService.syncRapla error: $e');
      rethrow;
    }
  }

  Future<void> updateEventNotes(int eventId, String notes) async {
    try {
      await _apiClient.patch(
        '${ApiConfig.calendar}/$eventId/notes',
        data: {'notes': notes},
      );
    } catch (e) {
      print('CalendarService.updateEventNotes error: $e');
      rethrow;
    }
  }
}
