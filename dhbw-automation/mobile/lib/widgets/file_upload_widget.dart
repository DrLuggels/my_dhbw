import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'dart:io';
import '../../core/services/file_service.dart';

class FileUploadWidget extends StatefulWidget {
  const FileUploadWidget({super.key});

  @override
  State<FileUploadWidget> createState() => _FileUploadWidgetState();
}

class _FileUploadWidgetState extends State<FileUploadWidget> {
  bool _isUploading = false;
  double _uploadProgress = 0.0;

  Future<void> _pickAndUpload(BuildContext context, {bool fromCamera = false}) async {
    final fileService = context.read<FileService>();
    
    final File? file = fromCamera
        ? await fileService.pickImageFromCamera()
        : await fileService.pickImageFromGallery();

    if (file == null || !mounted) return;

    final category = await _showCategoryDialog();
    if (category == null || !mounted) return;

    setState(() {
      _isUploading = true;
      _uploadProgress = 0.0;
    });

    try {
      await fileService.uploadWithProgress(
        file,
        category: category,
        onProgress: (sent, total) {
          setState(() {
            _uploadProgress = sent / total;
          });
        },
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Datei hochgeladen und wird analysiert...'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Upload fehlgeschlagen: $e')),
        );
      }
    } finally {
      setState(() {
        _isUploading = false;
        _uploadProgress = 0.0;
      });
    }
  }

  Future<String?> _showCategoryDialog() async {
    return await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Kategorie wählen'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _CategoryOption('lecture', 'Vorlesung', Icons.school),
            _CategoryOption('assignment', 'Aufgabe', Icons.assignment),
            _CategoryOption('notes', 'Notizen', Icons.note),
            _CategoryOption('exam', 'Prüfung', Icons.quiz),
            _CategoryOption('other', 'Sonstiges', Icons.folder),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isUploading) {
      return Card(
        margin: const EdgeInsets.all(16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Upload läuft...'),
              const SizedBox(height: 8),
              LinearProgressIndicator(value: _uploadProgress),
              const SizedBox(height: 8),
              Text('${(_uploadProgress * 100).toInt()}%'),
            ],
          ),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ElevatedButton.icon(
            onPressed: () => _pickAndUpload(context, fromCamera: true),
            icon: const Icon(Icons.camera_alt),
            label: const Text('Foto aufnehmen'),
            style: ElevatedButton.styleFrom(
              minimumSize: const Size(double.infinity, 50),
            ),
          ),
          const SizedBox(height: 12),
          ElevatedButton.icon(
            onPressed: () => _pickAndUpload(context, fromCamera: false),
            icon: const Icon(Icons.photo_library),
            label: const Text('Aus Galerie wählen'),
            style: ElevatedButton.styleFrom(
              minimumSize: const Size(double.infinity, 50),
            ),
          ),
        ],
      ),
    );
  }
}

class _CategoryOption extends StatelessWidget {
  final String value;
  final String label;
  final IconData icon;

  const _CategoryOption(this.value, this.label, this.icon);

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon),
      title: Text(label),
      onTap: () => Navigator.pop(context, value),
    );
  }
}
