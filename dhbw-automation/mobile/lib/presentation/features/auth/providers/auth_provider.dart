import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/storage/secure_storage.dart';
import '../../../../data/models/auth/user_model.dart';
import '../../../../data/repositories/auth_repository.dart';

part 'auth_provider.freezed.dart';

/// Auth State
@freezed
class AuthState with _$AuthState {
  const factory AuthState({
    UserModel? user,
    @Default(false) bool isLoading,
    String? error,
  }) = _AuthState;

  const AuthState._();

  /// Check if user is authenticated
  bool get isAuthenticated => user != null;
}

/// Secure Storage Provider
final secureStorageProvider = Provider<SecureStorage>((ref) {
  return SecureStorage();
});

/// Dio Client Provider
final dioClientProvider = Provider<DioClient>((ref) {
  final secureStorage = ref.watch(secureStorageProvider);
  return DioClient(secureStorage);
});

/// Auth Repository Provider
final authRepositoryProvider = Provider<AuthRepository>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return AuthRepository(dioClient);
});

/// Auth State Notifier
class AuthNotifier extends StateNotifier<AuthState> {
  final AuthRepository _authRepository;
  final SecureStorage _secureStorage;

  AuthNotifier(this._authRepository, this._secureStorage)
      : super(const AuthState()) {
    // Load user from storage on init
    _loadFromStorage();
  }

  /// Load user from secure storage
  Future<void> _loadFromStorage() async {
    try {
      final token = await _secureStorage.getToken();
      final userJson = await _secureStorage.getUser();

      if (token != null && userJson != null) {
        final user = UserModel.fromJson(jsonDecode(userJson));
        state = state.copyWith(user: user);
      }
    } catch (e) {
      // If loading fails, clear storage
      await _secureStorage.clearAll();
    }
  }

  /// Login with email and password
  Future<bool> login(String email, String password) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _authRepository.login(email, password);

      if (response.success && response.data != null) {
        final authData = response.data!;

        // Save token to secure storage
        await _secureStorage.saveToken(authData.token);

        // Save user data to secure storage
        await _secureStorage.saveUser(jsonEncode(authData.user.toJson()));

        // Update state
        state = state.copyWith(
          user: authData.user,
          isLoading: false,
          error: null,
        );

        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Login fehlgeschlagen',
          isLoading: false,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isLoading: false,
      );
      return false;
    }
  }

  /// Register new user
  Future<bool> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? matriculationNumber,
    String? course,
  }) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _authRepository.register(
        email: email,
        password: password,
        firstName: firstName,
        lastName: lastName,
        matriculationNumber: matriculationNumber,
        course: course,
      );

      if (response.success && response.data != null) {
        final authData = response.data!;

        // Save token to secure storage
        await _secureStorage.saveToken(authData.token);

        // Save user data to secure storage
        await _secureStorage.saveUser(jsonEncode(authData.user.toJson()));

        // Update state
        state = state.copyWith(
          user: authData.user,
          isLoading: false,
          error: null,
        );

        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Registrierung fehlgeschlagen',
          isLoading: false,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isLoading: false,
      );
      return false;
    }
  }

  /// Logout
  Future<void> logout() async {
    // Clear secure storage
    await _secureStorage.deleteToken();
    await _secureStorage.deleteUser();

    // Reset state
    state = const AuthState();
  }

  /// Clear error
  void clearError() {
    state = state.copyWith(error: null);
  }
}

/// Auth State Provider
final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  final authRepository = ref.watch(authRepositoryProvider);
  final secureStorage = ref.watch(secureStorageProvider);
  return AuthNotifier(authRepository, secureStorage);
});
