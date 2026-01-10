import 'package:freezed_annotation/freezed_annotation.dart';

part 'ai_question_model.freezed.dart';
part 'ai_question_model.g.dart';

/// AI Question Model
@freezed
class AIQuestionModel with _$AIQuestionModel {
  const factory AIQuestionModel({
    required int id,
    required int stagedEntityId,
    required String fieldName,
    required String questionText,
    required String answerType, // 'text', 'date', 'time', 'datetime', 'choice', 'number'
    required String priority, // 'critical', 'high', 'medium', 'low'
    required bool isAnswered,
    String? userAnswer,
    String? suggestedAnswers, // JSON array string
    String? answeredAt,
  }) = _AIQuestionModel;

  factory AIQuestionModel.fromJson(Map<String, dynamic> json) =>
      _$AIQuestionModelFromJson(json);
}
