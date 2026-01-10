import 'package:dio/dio.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/validation/staged_entity_model.dart';

/// Validation Repository
/// Handles validation and entity confirmation
class ValidationRepository {
  final DioClient _dioClient;

  ValidationRepository(this._dioClient);

  /// Get pending entities for review
  Future<ApiResponse<List<StagedEntityModel>>> getPendingEntities({
    String? status,
  }) async {
    try {
      final queryParams = status != null ? {'status': status} : null;

      final response = await _dioClient.get(
        ApiConstants.pendingEntities,
        queryParameters: queryParams,
      );

      // Extract entities from response
      final data = response.data;
      List<StagedEntityModel> entities;

      if (data is Map<String, dynamic> && data['entities'] != null) {
        entities = (data['entities'] as List)
            .map((item) => StagedEntityModel.fromJson(item as Map<String, dynamic>))
            .toList();
      } else if (data is List) {
        entities = data
            .map((item) => StagedEntityModel.fromJson(item as Map<String, dynamic>))
            .toList();
      } else {
        entities = [];
      }

      return ApiResponse(
        success: true,
        data: entities,
        message: data is Map ? data['message'] : null,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Get single staged entity
  Future<ApiResponse<StagedEntityModel>> getStagedEntity(int id) async {
    try {
      final response = await _dioClient.get(
        '${ApiConstants.answerQuestions}/$id',
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => StagedEntityModel.fromJson(json as Map<String, dynamic>),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Answer questions for entity
  Future<ApiResponse<void>> answerQuestions(
    int id,
    Map<String, String> answers,
  ) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.answerQuestions}/$id/answer',
        data: {'answers': answers},
      );

      return ApiResponse(
        success: response.data['success'] ?? true,
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Confirm entity
  Future<ApiResponse<int>> confirmEntity(
    int id, {
    String? userNotes,
  }) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.confirmEntity}/$id/confirm',
        data: userNotes != null ? {'userNotes': userNotes} : null,
      );

      return ApiResponse(
        success: response.data['success'] ?? true,
        data: response.data['promotedEntityId'],
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Reject entity
  Future<ApiResponse<void>> rejectEntity(
    int id, {
    String? reason,
  }) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.confirmEntity}/$id/reject',
        data: reason != null ? {'reason': reason} : null,
      );

      return ApiResponse(
        success: response.data['success'] ?? true,
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Bulk confirm entities
  Future<ApiResponse<Map<String, int>>> bulkConfirm({
    int minConfidence = 95,
  }) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.bulkConfirm}?minConfidence=$minConfidence',
      );

      return ApiResponse(
        success: response.data['success'] ?? true,
        data: {
          'promotedCount': response.data['promotedCount'] ?? 0,
          'totalEligible': response.data['totalEligible'] ?? 0,
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
