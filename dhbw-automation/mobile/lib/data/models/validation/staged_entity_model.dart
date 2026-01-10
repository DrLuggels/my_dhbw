import 'package:freezed_annotation/freezed_annotation.dart';
import 'ai_question_model.dart';

part 'staged_entity_model.freezed.dart';
part 'staged_entity_model.g.dart';

/// Staged Entity Model
@freezed
class StagedEntityModel with _$StagedEntityModel {
  const factory StagedEntityModel({
    required int id,
    required int userId,
    required String entityType, // 'todo', 'meeting', 'project', 'learning_deficit', 'reminder'
    required String entityData, // JSON string
    required int confidenceScore, // 0-100
    required String status, // 'pending_review', 'confirmed', 'modified', 'rejected'
    required String priority, // 'low', 'medium', 'high', 'urgent'
    required bool isPromoted,
    int? promotedEntityId,
    int? sourceDocumentId,
    @Default([]) List<AIQuestionModel> questions,
    String? createdAt,
    String? reviewedAt,
  }) = _StagedEntityModel;

  factory StagedEntityModel.fromJson(Map<String, dynamic> json) =>
      _$StagedEntityModelFromJson(json);
}
