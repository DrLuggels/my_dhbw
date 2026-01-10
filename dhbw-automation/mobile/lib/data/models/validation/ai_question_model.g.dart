// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ai_question_model.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$AIQuestionModelImpl _$$AIQuestionModelImplFromJson(
        Map<String, dynamic> json) =>
    _$AIQuestionModelImpl(
      id: (json['id'] as num).toInt(),
      stagedEntityId: (json['stagedEntityId'] as num).toInt(),
      fieldName: json['fieldName'] as String,
      questionText: json['questionText'] as String,
      answerType: json['answerType'] as String,
      priority: json['priority'] as String,
      isAnswered: json['isAnswered'] as bool,
      userAnswer: json['userAnswer'] as String?,
      suggestedAnswers: json['suggestedAnswers'] as String?,
      answeredAt: json['answeredAt'] as String?,
    );

Map<String, dynamic> _$$AIQuestionModelImplToJson(
        _$AIQuestionModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'stagedEntityId': instance.stagedEntityId,
      'fieldName': instance.fieldName,
      'questionText': instance.questionText,
      'answerType': instance.answerType,
      'priority': instance.priority,
      'isAnswered': instance.isAnswered,
      'userAnswer': instance.userAnswer,
      'suggestedAnswers': instance.suggestedAnswers,
      'answeredAt': instance.answeredAt,
    };
