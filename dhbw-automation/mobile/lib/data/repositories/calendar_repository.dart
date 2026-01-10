import 'package:dio/dio.dart';
import 'package:hive/hive.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/calendar/calendar_event_model.dart';
import '../local/hive_boxes.dart';

/// Calendar Repository
/// Handles calendar events with offline support
class CalendarRepository {
  final DioClient _dioClient;
  final Box<CalendarEventModel> _eventsBox;

  CalendarRepository(this._dioClient)
      : _eventsBox = HiveBoxes().getEventsBox();

  /// Get events for user (with offline fallback)
  Future<ApiResponse<List<CalendarEventModel>>> getEvents(
    int userId, {
    DateTime? startDate,
    DateTime? endDate,
    String? source,
  }) async {
    try {
      // Build query parameters
      final queryParams = <String, dynamic>{};
      if (startDate != null) {
        queryParams['startDate'] = startDate.toIso8601String();
      }
      if (endDate != null) {
        queryParams['endDate'] = endDate.toIso8601String();
      }
      if (source != null) {
        queryParams['source'] = source;
      }

      // API Call
      final response = await _dioClient.get(
        '${ApiConstants.getEvents}/$userId',
        queryParameters: queryParams,
      );

      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => (json as List)
            .map((item) => CalendarEventModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );

      // Update Hive cache
      if (apiResponse.success && apiResponse.data != null) {
        await _eventsBox.clear();
        for (var event in apiResponse.data!) {
          await _eventsBox.put(event.id, event);
        }
      }

      return apiResponse;
    } on DioException catch (e) {
      // Offline fallback
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout) {
        final cachedEvents = _eventsBox.values.toList();

        // Filter cached events if dates are provided
        List<CalendarEventModel> filteredEvents = cachedEvents;
        if (startDate != null || endDate != null) {
          filteredEvents = cachedEvents.where((event) {
            if (startDate != null && event.startTime.isBefore(startDate)) {
              return false;
            }
            if (endDate != null && event.startTime.isAfter(endDate)) {
              return false;
            }
            return true;
          }).toList();
        }

        return ApiResponse(
          success: true,
          data: filteredEvents,
          message: 'Offline: ${filteredEvents.length} Events aus Cache',
        );
      }
      throw _handleError(e);
    }
  }

  /// Get week schedule
  Future<ApiResponse<List<CalendarEventModel>>> getWeekSchedule({
    DateTime? weekStart,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (weekStart != null) {
        queryParams['weekStart'] = weekStart.toIso8601String();
      }

      final response = await _dioClient.get(
        ApiConstants.weekSchedule,
        queryParameters: queryParams,
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => (json as List)
            .map((item) => CalendarEventModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Sync Rapla calendar
  Future<ApiResponse<Map<String, int>>> syncRapla(int userId) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.syncRapla}/$userId',
      );

      return ApiResponse(
        success: response.data['success'] ?? false,
        data: {
          'newEvents': response.data['newEvents'] ?? 0,
          'updatedEvents': response.data['updatedEvents'] ?? 0,
        },
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Handle Dio errors
  Exception _handleError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      String errorMessage = 'Ein Fehler ist aufgetreten';

      if (data is Map<String, dynamic>) {
        if (data['message'] != null) {
          errorMessage = data['message'];
        } else if (data['errors'] != null && data['errors'] is List) {
          errorMessage = (data['errors'] as List).join(', ');
        }
      }

      return Exception(errorMessage);
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return Exception('Zeitüberschreitung der Verbindung');
    } else if (e.type == DioExceptionType.connectionError) {
      return Exception('Keine Verbindung zum Server möglich');
    }

    return Exception('Netzwerkfehler: ${e.message}');
  }
}
