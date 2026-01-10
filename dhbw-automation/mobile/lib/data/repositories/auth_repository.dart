import 'package:dio/dio.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/auth/auth_response_model.dart';

/// Authentication Repository
/// Handles all authentication-related API calls
class AuthRepository {
  final DioClient _dioClient;

  AuthRepository(this._dioClient);

  /// Login with email and password
  Future<ApiResponse<AuthResponseModel>> login(
    String email,
    String password,
  ) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.login,
        data: {
          'email': email,
          'password': password,
        },
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Register new user
  Future<ApiResponse<AuthResponseModel>> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? matriculationNumber,
    String? course,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.register,
        data: {
          'email': email,
          'password': password,
          'firstName': firstName,
          'lastName': lastName,
          if (matriculationNumber != null)
            'matriculationNumber': matriculationNumber,
          if (course != null) 'course': course,
        },
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Change password
  Future<ApiResponse<void>> changePassword(
    String oldPassword,
    String newPassword,
  ) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.changePassword,
        data: {
          'oldPassword': oldPassword,
          'newPassword': newPassword,
        },
      );

      return ApiResponse(
        success: response.data['success'] ?? false,
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Handle Dio errors and convert to readable exceptions
  Exception _handleError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;

      // Extract error message from response
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
