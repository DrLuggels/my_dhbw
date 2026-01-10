import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';
import '../../../../data/models/files/document_model.dart';
import '../../../../data/repositories/files_repository.dart';
import '../../auth/providers/auth_provider.dart';

part 'files_provider.freezed.dart';

/// Files State
@freezed
class FilesState with _$FilesState {
  const factory FilesState({
    @Default([]) List<DocumentModel> documents,
    @Default(false) bool isLoading,
    @Default(false) bool isUploading,
    @Default(0.0) double uploadProgress,
    String? error,
    String? successMessage,
  }) = _FilesState;
}

/// Files Repository Provider
final filesRepositoryProvider = Provider<FilesRepository>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return FilesRepository(dioClient);
});

/// Files State Notifier
class FilesNotifier extends StateNotifier<FilesState> {
  final FilesRepository _filesRepository;

  FilesNotifier(this._filesRepository) : super(const FilesState()) {
    // Load files on init
    loadFiles();
  }

  /// Load all files
  Future<void> loadFiles() async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _filesRepository.getFiles();

      if (response.success && response.data != null) {
        state = state.copyWith(
          documents: response.data!,
          isLoading: false,
          successMessage: response.message,
        );
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Laden der Dateien',
          isLoading: false,
        );
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isLoading: false,
      );
    }
  }

  /// Upload file with progress tracking
  Future<bool> uploadFile(
    File file, {
    String? category,
  }) async {
    state = state.copyWith(
      isUploading: true,
      uploadProgress: 0.0,
      error: null,
      successMessage: null,
    );

    try {
      final response = await _filesRepository.uploadFile(
        file,
        category: category,
        onUploadProgress: (sent, total) {
          final progress = sent / total;
          state = state.copyWith(uploadProgress: progress);
        },
      );

      if (response.success) {
        state = state.copyWith(
          isUploading: false,
          uploadProgress: 0.0,
          successMessage: 'Datei erfolgreich hochgeladen!',
        );

        // Reload files
        await loadFiles();
        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Upload fehlgeschlagen',
          isUploading: false,
          uploadProgress: 0.0,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isUploading: false,
        uploadProgress: 0.0,
      );
      return false;
    }
  }

  /// Delete file
  Future<bool> deleteFile(int fileId) async {
    try {
      final response = await _filesRepository.deleteFile(fileId);

      if (response.success) {
        // Remove from local state
        state = state.copyWith(
          documents: state.documents.where((doc) => doc.id != fileId).toList(),
          successMessage: 'Datei gelöscht',
        );
        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Löschen fehlgeschlagen',
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
      );
      return false;
    }
  }

  /// Bulk delete files
  Future<bool> bulkDeleteFiles(List<int> fileIds) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _filesRepository.bulkDeleteFiles(fileIds);

      if (response.success) {
        // Remove from local state
        state = state.copyWith(
          documents: state.documents
              .where((doc) => !fileIds.contains(doc.id))
              .toList(),
          isLoading: false,
          successMessage:
              '${response.data?['successCount'] ?? 0} Dateien gelöscht',
        );
        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Bulk-Löschen fehlgeschlagen',
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

  /// Clear messages
  void clearMessages() {
    state = state.copyWith(error: null, successMessage: null);
  }
}

/// Files State Provider
final filesProvider = StateNotifierProvider<FilesNotifier, FilesState>((ref) {
  final filesRepository = ref.watch(filesRepositoryProvider);
  return FilesNotifier(filesRepository);
});
