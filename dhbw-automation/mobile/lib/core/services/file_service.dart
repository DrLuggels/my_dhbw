import 'dart:io';
import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';
import '../config/api_config.dart';
import 'api_client.dart';

class FileService {
  final ApiClient _apiClient;
  final ImagePicker _picker = ImagePicker();

  FileService(this._apiClient);

  Future<File?> pickImageFromCamera() async {
    try {
      final XFile? image = await _picker.pickImage(
        source: ImageSource.camera,
        imageQuality: 80,
      );
      return image != null ? File(image.path) : null;
    } catch (e) {
      print('FileService.pickImageFromCamera error: $e');
      return null;
    }
  }

  Future<File?> pickImageFromGallery() async {
    try {
      final XFile? image = await _picker.pickImage(
        source: ImageSource.gallery,
        imageQuality: 80,
      );
      return image != null ? File(image.path) : null;
    } catch (e) {
      print('FileService.pickImageFromGallery error: $e');
      return null;
    }
  }

  Future<void> uploadFile(File file, {String? category}) async {
    try {
      final fileName = file.path.split('/').last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          file.path,
          filename: fileName,
        ),
        if (category != null) 'category': category,
      });

      await _apiClient.post(
        '${ApiConfig.files}/upload',
        data: formData,
      );
    } catch (e) {
      print('FileService.uploadFile error: $e');
      rethrow;
    }
  }

  Future<void> uploadWithProgress(
    File file, {
    String? category,
    Function(int sent, int total)? onProgress,
  }) async {
    try {
      final fileName = file.path.split('/').last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          file.path,
          filename: fileName,
        ),
        if (category != null) 'category': category,
      });

      await _apiClient.dio.post(
        '${ApiConfig.files}/upload',
        data: formData,
        onSendProgress: onProgress,
      );
    } catch (e) {
      print('FileService.uploadWithProgress error: $e');
      rethrow;
    }
  }
}
