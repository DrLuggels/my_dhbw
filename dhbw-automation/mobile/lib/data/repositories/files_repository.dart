import 'dart:io';
import 'package:dio/dio.dart';
import 'package:hive/hive.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/files/document_model.dart';
import '../local/hive_boxes.dart';

/// Files Repository
/// Handles file upload, download, and management with offline support
class FilesRepository {
  final DioClient _dioClient;
  final Box<DocumentModel> _documentsBox;

  FilesRepository(this._dioClient)
      : _documentsBox = HiveBoxes().getDocumentsBox();

  /// Upload file with progress callback
  /// Matches Vue.js api.ts uploadFile pattern (line 76-96)
  Future<ApiResponse<DocumentModel>> uploadFile(
    File file, {
    String? category,
    ProgressCallback? onUploadProgress,
  }) async {
    try {
      // Create FormData (matches Vue.js pattern)
      final fileName = file.path.split('/').last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          file.path,
          filename: fileName,
        ),
        if (category != null) 'category': category,
      });

      // Upload with progress tracking
      final response = await _dioClient.uploadFile(
        ApiConstants.uploadFile,
        formData,
        onSendProgress: onUploadProgress,
      );

      // Parse response
      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => DocumentModel.fromJson(json as Map<String, dynamic>),
      );

      // Save to Hive for offline access
      if (apiResponse.success && apiResponse.data != null) {
        await _documentsBox.put(apiResponse.data!.id, apiResponse.data!);
      }

      return apiResponse;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Get all files (with offline fallback)
  Future<ApiResponse<List<DocumentModel>>> getFiles() async {
    try {
      // Try to fetch from API
      final response = await _dioClient.get(ApiConstants.getFiles);

      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => (json as List)
            .map((item) => DocumentModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );

      // Update Hive cache
      if (apiResponse.success && apiResponse.data != null) {
        await _documentsBox.clear();
        for (var doc in apiResponse.data!) {
          await _documentsBox.put(doc.id, doc);
        }
      }

      return apiResponse;
    } on DioException catch (e) {
      // Offline fallback: return cached data
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout) {
        final cachedDocs = _documentsBox.values.toList();
        return ApiResponse(
          success: true,
          data: cachedDocs,
          message: 'Offline: ${cachedDocs.length} Dokumente aus Cache',
        );
      }
      throw _handleError(e);
    }
  }

  /// Download file
  Future<File> downloadFile(int fileId, String savePath) async {
    try {
      final response = await _dioClient.get(
        '${ApiConstants.downloadFile}/$fileId',
        options: Options(responseType: ResponseType.bytes),
      );

      // Save to file
      final file = File(savePath);
      await file.writeAsBytes(response.data);
      return file;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Delete file
  Future<ApiResponse<bool>> deleteFile(int fileId) async {
    try {
      final response = await _dioClient.delete(
        '${ApiConstants.deleteFile}/$fileId',
      );

      // Remove from Hive cache
      await _documentsBox.delete(fileId);

      return ApiResponse(
        success: response.data['success'] ?? false,
        data: true,
        message: response.data['message'],
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Bulk delete files
  Future<ApiResponse<Map<String, int>>> bulkDeleteFiles(
    List<int> fileIds,
  ) async {
    try {
      final response = await _dioClient.post(
        ApiConstants.bulkDeleteFiles,
        data: fileIds,
      );

      // Remove from Hive cache
      for (var id in fileIds) {
        await _documentsBox.delete(id);
      }

      return ApiResponse(
        success: response.data['success'] ?? false,
        data: {
          'successCount': response.data['successCount'] ?? 0,
          'failureCount': response.data['failureCount'] ?? 0,
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
