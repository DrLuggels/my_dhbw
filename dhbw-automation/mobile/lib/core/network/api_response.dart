/// Generic API Response wrapper
/// Matches the backend ApiResponse<T> structure
class ApiResponse<T> {
  final bool success;
  final T? data;
  final String? message;
  final List<String>? errors;

  ApiResponse({
    required this.success,
    this.data,
    this.message,
    this.errors,
  });

  /// Create ApiResponse from JSON with generic type converter
  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) {
    return ApiResponse<T>(
      success: json['success'] ?? false,
      data: json['data'] != null ? fromJsonT(json['data']) : null,
      message: json['message'],
      errors: json['errors'] != null
          ? List<String>.from(json['errors'])
          : null,
    );
  }

  /// Check if response is successful and has data
  bool get hasData => success && data != null;

  /// Get error message (first error or message)
  String get errorMessage {
    if (errors != null && errors!.isNotEmpty) {
      return errors!.first;
    }
    return message ?? 'Ein Fehler ist aufgetreten';
  }

  @override
  String toString() {
    return 'ApiResponse{success: $success, data: $data, message: $message, errors: $errors}';
  }
}
