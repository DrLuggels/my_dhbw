import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:permission_handler/permission_handler.dart';
import '../providers/files_provider.dart';

class FilesScreen extends ConsumerStatefulWidget {
  const FilesScreen({super.key});

  @override
  ConsumerState<FilesScreen> createState() => _FilesScreenState();
}

class _FilesScreenState extends ConsumerState<FilesScreen> {
  final ImagePicker _imagePicker = ImagePicker();
  List<int> _selectedFileIds = [];
  bool _selectMode = false;

  @override
  Widget build(BuildContext context) {
    final filesState = ref.watch(filesProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(
          _selectMode
              ? '${_selectedFileIds.length} ausgewählt'
              : 'Dateien',
        ),
        actions: [
          if (!_selectMode)
            IconButton(
              icon: const Icon(Icons.checklist),
              onPressed: () {
                setState(() {
                  _selectMode = true;
                });
              },
              tooltip: 'Auswählen',
            ),
          if (_selectMode) ...[
            IconButton(
              icon: const Icon(Icons.select_all),
              onPressed: () {
                setState(() {
                  _selectedFileIds =
                      filesState.documents.map((doc) => doc.id).toList();
                });
              },
              tooltip: 'Alle auswählen',
            ),
            IconButton(
              icon: const Icon(Icons.delete),
              onPressed: _selectedFileIds.isEmpty
                  ? null
                  : () => _handleBulkDelete(),
              tooltip: 'Löschen',
            ),
            IconButton(
              icon: const Icon(Icons.close),
              onPressed: () {
                setState(() {
                  _selectMode = false;
                  _selectedFileIds.clear();
                });
              },
              tooltip: 'Abbrechen',
            ),
          ],
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: filesState.isLoading
                ? null
                : () => ref.read(filesProvider.notifier).loadFiles(),
            tooltip: 'Aktualisieren',
          ),
        ],
      ),
      body: Column(
        children: [
          // Upload Progress Indicator
          if (filesState.isUploading)
            LinearProgressIndicator(
              value: filesState.uploadProgress,
            ),

          // Success/Error Messages
          if (filesState.successMessage != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.green.shade50,
              child: Row(
                children: [
                  Icon(Icons.check_circle, color: Colors.green.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      filesState.successMessage!,
                      style: TextStyle(color: Colors.green.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(filesProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          if (filesState.error != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.red.shade50,
              child: Row(
                children: [
                  Icon(Icons.error_outline, color: Colors.red.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      filesState.error!,
                      style: TextStyle(color: Colors.red.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(filesProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          // Files List
          Expanded(
            child: filesState.isLoading && filesState.documents.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : filesState.documents.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              Icons.folder_open,
                              size: 100,
                              color: Colors.grey.shade300,
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'Keine Dateien',
                              style: TextStyle(
                                fontSize: 18,
                                color: Colors.grey.shade600,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Tippe auf + um Dateien hochzuladen',
                              style: TextStyle(color: Colors.grey.shade500),
                            ),
                          ],
                        ),
                      )
                    : ListView.builder(
                        itemCount: filesState.documents.length,
                        itemBuilder: (context, index) {
                          final doc = filesState.documents[index];
                          final isSelected = _selectedFileIds.contains(doc.id);

                          return ListTile(
                            leading: _selectMode
                                ? Checkbox(
                                    value: isSelected,
                                    onChanged: (checked) {
                                      setState(() {
                                        if (checked == true) {
                                          _selectedFileIds.add(doc.id);
                                        } else {
                                          _selectedFileIds.remove(doc.id);
                                        }
                                      });
                                    },
                                  )
                                : _getFileIcon(doc.fileType),
                            title: Text(
                              doc.fileName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  '${doc.fileType.toUpperCase()} • ${_formatSize(doc.fileSize)}',
                                ),
                                if (doc.isProcessed)
                                  Row(
                                    children: [
                                      Icon(
                                        Icons.check_circle,
                                        size: 14,
                                        color: Colors.green.shade600,
                                      ),
                                      const SizedBox(width: 4),
                                      Text(
                                        'Verarbeitet',
                                        style: TextStyle(
                                          color: Colors.green.shade600,
                                          fontSize: 12,
                                        ),
                                      ),
                                    ],
                                  )
                                else
                                  Row(
                                    children: [
                                      SizedBox(
                                        width: 12,
                                        height: 12,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: Colors.orange.shade600,
                                        ),
                                      ),
                                      const SizedBox(width: 6),
                                      Text(
                                        'Wird verarbeitet...',
                                        style: TextStyle(
                                          color: Colors.orange.shade600,
                                          fontSize: 12,
                                        ),
                                      ),
                                    ],
                                  ),
                              ],
                            ),
                            trailing: _selectMode
                                ? null
                                : IconButton(
                                    icon: const Icon(Icons.delete_outline),
                                    onPressed: () => _handleDelete(doc.id),
                                  ),
                            onTap: _selectMode
                                ? () {
                                    setState(() {
                                      if (isSelected) {
                                        _selectedFileIds.remove(doc.id);
                                      } else {
                                        _selectedFileIds.add(doc.id);
                                      }
                                    });
                                  }
                                : null,
                          );
                        },
                      ),
          ),
        ],
      ),
      floatingActionButton: filesState.isUploading
          ? null
          : FloatingActionButton(
              onPressed: _showUploadOptions,
              child: const Icon(Icons.add_a_photo),
            ),
    );
  }

  Widget _getFileIcon(String fileType) {
    IconData icon;
    Color color;

    switch (fileType.toLowerCase()) {
      case 'pdf':
        icon = Icons.picture_as_pdf;
        color = Colors.red;
        break;
      case 'docx':
      case 'doc':
        icon = Icons.description;
        color = Colors.blue;
        break;
      case 'txt':
        icon = Icons.text_snippet;
        color = Colors.grey;
        break;
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        icon = Icons.image;
        color = Colors.purple;
        break;
      default:
        icon = Icons.insert_drive_file;
        color = Colors.grey;
    }

    return Icon(icon, color: color);
  }

  String _formatSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) {
      return '${(bytes / 1024).toStringAsFixed(1)} KB';
    }
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  Future<void> _showUploadOptions() async {
    showModalBottomSheet(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.camera_alt),
              title: const Text('Foto aufnehmen'),
              onTap: () {
                Navigator.pop(context);
                _pickImageFromCamera();
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library),
              title: const Text('Aus Galerie wählen'),
              onTap: () {
                Navigator.pop(context);
                _pickImageFromGallery();
              },
            ),
            ListTile(
              leading: const Icon(Icons.cancel),
              title: const Text('Abbrechen'),
              onTap: () => Navigator.pop(context),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickImageFromCamera() async {
    // Request camera permission
    final status = await Permission.camera.request();

    if (status.isGranted) {
      final XFile? photo = await _imagePicker.pickImage(
        source: ImageSource.camera,
        imageQuality: 85,
      );

      if (photo != null) {
        await _uploadFile(File(photo.path));
      }
    } else {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Kamera-Berechtigung erforderlich'),
          ),
        );
      }
    }
  }

  Future<void> _pickImageFromGallery() async {
    final XFile? image = await _imagePicker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 85,
    );

    if (image != null) {
      await _uploadFile(File(image.path));
    }
  }

  Future<void> _uploadFile(File file) async {
    await ref.read(filesProvider.notifier).uploadFile(file);
  }

  Future<void> _handleDelete(int fileId) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Datei löschen?'),
        content: const Text(
          'Möchtest du diese Datei wirklich löschen? Dieser Vorgang kann nicht rückgängig gemacht werden.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await ref.read(filesProvider.notifier).deleteFile(fileId);
    }
  }

  Future<void> _handleBulkDelete() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Dateien löschen?'),
        content: Text(
          'Möchtest du ${_selectedFileIds.length} Dateien wirklich löschen? Dieser Vorgang kann nicht rückgängig gemacht werden.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      final success =
          await ref.read(filesProvider.notifier).bulkDeleteFiles(_selectedFileIds);

      if (success) {
        setState(() {
          _selectMode = false;
          _selectedFileIds.clear();
        });
      }
    }
  }
}
