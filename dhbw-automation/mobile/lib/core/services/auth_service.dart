import 'package:dio/dio.dart';
import '../config/api_config.dart';
import '../models/api_response.dart';
import '../models/user.dart';
import 'api_client.dart';
import 'storage_service.dart';

class AuthService {
  final ApiClient _apiClient;
  final StorageService _storage;

  AuthService(this._apiClient, this._storage);

  Future<AuthResponse> login(String email, String password) async {
    try {
      final response = await _apiClient.post(
        '${ApiConfig.auth}/login',
        data: {
          'email': email,
          'password': password,
        },
      );

      final apiResponse = ApiResponse<AuthResponse>.fromJson(
        response.data,
        (json) => AuthResponse.fromJson(json),
      );

      if (!apiResponse.success || apiResponse.data == null) {
        throw Exception(apiResponse.message ?? 'Login fehlgeschlagen');
      }

      final authData = apiResponse.data!;
      await _storage.saveToken(authData.token);
      await _storage.saveUserId(authData.user.id);

      return authData;
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Verbindungsfehler');
    }
  }

  Future<AuthResponse> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    try {
      final response = await _apiClient.post(
        '${ApiConfig.auth}/register',
        data: {
          'email': email,
          'password': password,
          'firstName': firstName,
          'lastName': lastName,
        },
      );

      final apiResponse = ApiResponse<AuthResponse>.fromJson(
        response.data,
        (json) => AuthResponse.fromJson(json),
      );

      if (!apiResponse.success || apiResponse.data == null) {
        throw Exception(apiResponse.message ?? 'Registrierung fehlgeschlagen');
      }

      final authData = apiResponse.data!;
      await _storage.saveToken(authData.token);
      await _storage.saveUserId(authData.user.id);

      return authData;
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Verbindungsfehler');
    }
  }

  Future<void> logout() async {
    await _storage.clearToken();
  }

  Future<bool> isLoggedIn() async {
    return await _storage.isLoggedIn();
  }
}
