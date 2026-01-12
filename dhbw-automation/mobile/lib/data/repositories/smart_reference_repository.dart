import 'package:dio/dio.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/smart_reference/smart_reference_models.dart';

/// Smart Reference Repository
/// Handles smart reference resolution and linking for notes
class SmartReferenceRepository {
  final DioClient _dioClient;

  SmartReferenceRepository(this._dioClient);

  /// Resolves natural language references in text
  /// Example: "der Prof von heute morgen" -> Professor + Event
  Future<ApiResponse<SmartReferenceResult>> resolveReferences({
    required String text,
    DateTime? referenceDate,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.smartReferenceResolve,
        data: {
          'text': text,
          if (referenceDate != null) 'referenceDate': referenceDate.toIso8601String(),
        },
      );

      return ApiResponse(
        success: true,
        data: SmartReferenceResult.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error resolving references',
      );
    }
  }

  /// Automatically links a note to related calendar events
  Future<ApiResponse<AutoLinkResult>> autoLinkNote({
    required int eventId,
    required String noteContent,
    bool autoConfirmHighConfidence = true,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.smartReferenceAutoLink,
        data: {
          'eventId': eventId,
          'noteContent': noteContent,
          'autoConfirmHighConfidence': autoConfirmHighConfidence,
        },
      );

      return ApiResponse(
        success: response.data['success'] ?? false,
        data: AutoLinkResult(
          success: response.data['success'] ?? false,
          linksCreated: response.data['linksCreated'] ?? 0,
          error: response.data['error'],
        ),
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error auto-linking note',
      );
    }
  }

  /// Gets suggested links for note content
  Future<ApiResponse<List<SuggestedLink>>> getSuggestions({
    required String noteContent,
    int? sourceEventId,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.smartReferenceSuggestions,
        data: {
          'noteContent': noteContent,
          if (sourceEventId != null) 'sourceEventId': sourceEventId,
        },
      );

      final suggestions = (response.data as List)
          .map((item) => SuggestedLink.fromJson(item as Map<String, dynamic>))
          .toList();

      return ApiResponse(
        success: true,
        data: suggestions,
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error getting suggestions',
      );
    }
  }

  /// Confirms a suggested link
  Future<ApiResponse<ConfirmLinkResult>> confirmLink({
    required String sourceType,
    required int sourceId,
    required String targetType,
    required int targetId,
    String linkType = 'related',
    double confidence = 1.0,
    String? reason,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.smartReferenceConfirm,
        data: {
          'sourceType': sourceType,
          'sourceId': sourceId,
          'targetType': targetType,
          'targetId': targetId,
          'linkType': linkType,
          'confidence': confidence,
          if (reason != null) 'reason': reason,
        },
      );

      return ApiResponse(
        success: response.data['success'] ?? false,
        data: ConfirmLinkResult(
          success: response.data['success'] ?? false,
          linkId: response.data['linkId'],
          error: response.data['error'],
        ),
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error confirming link',
      );
    }
  }

  /// Gets all links related to a specific calendar event
  Future<ApiResponse<List<KnowledgeLink>>> getLinksForEvent(int eventId) async {
    try {
      final response = await _dioClient.get(
        '${ApiConstants.smartReferenceEventLinks}/$eventId/links',
      );

      final links = (response.data as List)
          .map((item) => KnowledgeLink.fromJson(item as Map<String, dynamic>))
          .toList();

      return ApiResponse(
        success: true,
        data: links,
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error getting event links',
      );
    }
  }

  /// Parses a temporal expression to a date/time range
  Future<ApiResponse<TemporalParseResult>> parseTemporal({
    required String expression,
    DateTime? referenceDate,
  }) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.smartReferenceParseTemporal,
        data: {
          'expression': expression,
          if (referenceDate != null) 'referenceDate': referenceDate.toIso8601String(),
        },
      );

      return ApiResponse(
        success: true,
        data: TemporalParseResult.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error parsing temporal expression',
      );
    }
  }

  /// Gets the professor index for the current user
  Future<ApiResponse<Map<String, ProfessorInfo>>> getProfessors() async {
    try {
      final response = await _dioClient.get(ApiConstants.smartReferenceProfessors);

      final Map<String, ProfessorInfo> professors = {};
      final data = response.data as Map<String, dynamic>;
      for (var entry in data.entries) {
        professors[entry.key] = ProfessorInfo.fromJson(entry.value as Map<String, dynamic>);
      }

      return ApiResponse(
        success: true,
        data: professors,
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error getting professors',
      );
    }
  }

  /// Gets the subject index for the current user
  Future<ApiResponse<Map<String, SubjectInfo>>> getSubjects() async {
    try {
      final response = await _dioClient.get(ApiConstants.smartReferenceSubjects);

      final Map<String, SubjectInfo> subjects = {};
      final data = response.data as Map<String, dynamic>;
      for (var entry in data.entries) {
        subjects[entry.key] = SubjectInfo.fromJson(entry.value as Map<String, dynamic>);
      }

      return ApiResponse(
        success: true,
        data: subjects,
      );
    } on DioException catch (e) {
      return ApiResponse(
        success: false,
        message: e.message ?? 'Error getting subjects',
      );
    }
  }
}

/// Result of auto-link operation
class AutoLinkResult {
  final bool success;
  final int linksCreated;
  final String? error;

  AutoLinkResult({
    required this.success,
    required this.linksCreated,
    this.error,
  });
}

/// Result of confirm link operation
class ConfirmLinkResult {
  final bool success;
  final int? linkId;
  final String? error;

  ConfirmLinkResult({
    required this.success,
    this.linkId,
    this.error,
  });
}
