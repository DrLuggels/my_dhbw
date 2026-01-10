import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../constants/api_constants.dart';
import '../storage/secure_storage.dart';

/// Dio HTTP Client with JWT interceptors
/// Handles authentication, request/response logging, and error handling
class DioClient {
  late final Dio _dio;
  final SecureStorage _secureStorage;

  DioClient(this._secureStorage) {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConstants.baseUrl,
        connectTimeout: const Duration(milliseconds: ApiConstants.connectTimeout),
        receiveTimeout: const Duration(milliseconds: ApiConstants.receiveTimeout),
        headers: {
          'Content-Type': 'application/json',
        },
      ),
    );

    _initializeInterceptors();
  }

  void _initializeInterceptors() {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          // Add JWT Token from SecureStorage
          final token = await _secureStorage.getToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }

          // Remove Content-Type for FormData (let browser set it with boundary)
          // This matches the Vue.js pattern in api.ts:26-28
          if (options.data is FormData) {
            options.headers.remove('Content-Type');
          }

          // Debug logging in development mode
          if (kDebugMode) {
            print('┌─── REQUEST ───────────────────────────────────────');
            print('│ ${options.method} ${options.uri}');
            print('│ Headers: ${options.headers}');
            if (options.data != null && options.data is! FormData) {
              print('│ Body: ${options.data}');
            }
            print('└───────────────────────────────────────────────────');
          }

          return handler.next(options);
        },
        onResponse: (response, handler) {
          // Debug logging in development mode
          if (kDebugMode) {
            print('┌─── RESPONSE ──────────────────────────────────────');
            print('│ ${response.statusCode} ${response.requestOptions.uri}');
            print('│ Data: ${response.data}');
            print('└───────────────────────────────────────────────────');
          }

          return handler.next(response);
        },
        onError: (error, handler) async {
          // Handle 401 Unauthorized - Auto Logout
          // This matches the Vue.js pattern in api.ts:41-46
          if (error.response?.statusCode == 401) {
            if (kDebugMode) {
              print('┌─── 401 UNAUTHORIZED ──────────────────────────────');
              print('│ Auto-logout triggered');
              print('└───────────────────────────────────────────────────');
            }

            // Clear authentication data
            await _secureStorage.deleteToken();
            await _secureStorage.deleteUser();

            // Note: Navigation to login will be handled by go_router redirect
          }

          // Debug logging in development mode
          if (kDebugMode) {
            print('┌─── ERROR ─────────────────────────────────────────');
            print('│ ${error.requestOptions.method} ${error.requestOptions.uri}');
            print('│ Status: ${error.response?.statusCode}');
            print('│ Message: ${error.message}');
            print('│ Data: ${error.response?.data}');
            print('└───────────────────────────────────────────────────');
          }

          return handler.next(error);
        },
      ),
    );
  }

  // Expose Dio instance for use in repositories
  Dio get dio => _dio;

  // Helper method to handle file uploads with progress
  Future<Response> uploadFile(
    String path,
    FormData formData, {
    ProgressCallback? onSendProgress,
  }) async {
    return await _dio.post(
      path,
      data: formData,
      onSendProgress: onSendProgress,
    );
  }

  // Helper method for GET requests
  Future<Response> get(
    String path, {
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) async {
    return await _dio.get(
      path,
      queryParameters: queryParameters,
      options: options,
    );
  }

  // Helper method for POST requests
  Future<Response> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) async {
    return await _dio.post(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
    );
  }

  // Helper method for PUT requests
  Future<Response> put(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) async {
    return await _dio.put(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
    );
  }

  // Helper method for DELETE requests
  Future<Response> delete(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) async {
    return await _dio.delete(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
    );
  }

  // Helper method for PATCH requests
  Future<Response> patch(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
  }) async {
    return await _dio.patch(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
    );
  }
}
