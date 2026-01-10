import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:hive/hive.dart';

part 'document_model.freezed.dart';
part 'document_model.g.dart';

/// Document Model with Hive support for offline caching
@freezed
@HiveType(typeId: 1)
class DocumentModel with _$DocumentModel {
  const factory DocumentModel({
    @HiveField(0) required int id,
    @HiveField(1) required int userId,
    @HiveField(2) required String fileName,
    @HiveField(3) required String filePath,
    @HiveField(4) required String fileType,
    @HiveField(5) required int fileSize,
    @HiveField(6) required String category,
    @HiveField(7) @Default(false) bool isProcessed,
    @HiveField(8) String? summary,
    @HiveField(9) String? tags,
    @HiveField(10) String? extractedText,
    @HiveField(11) String? uploadedAt,
    @HiveField(12) String? processedAt,
  }) = _DocumentModel;

  factory DocumentModel.fromJson(Map<String, dynamic> json) =>
      _$DocumentModelFromJson(json);
}
