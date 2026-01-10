// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'staged_entity_model.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$StagedEntityModelImpl _$$StagedEntityModelImplFromJson(
        Map<String, dynamic> json) =>
    _$StagedEntityModelImpl(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      entityType: json['entityType'] as String,
      entityData: json['entityData'] as String,
      confidenceScore: (json['confidenceScore'] as num).toInt(),
      status: json['status'] as String,
      priority: json['priority'] as String,
      isPromoted: json['isPromoted'] as bool,
      promotedEntityId: (json['promotedEntityId'] as num?)?.toInt(),
      sourceDocumentId: (json['sourceDocumentId'] as num?)?.toInt(),
      questions: (json['questions'] as List<dynamic>?)
              ?.map((e) => AIQuestionModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
      createdAt: json['createdAt'] as String?,
      reviewedAt: json['reviewedAt'] as String?,
    );

Map<String, dynamic> _$$StagedEntityModelImplToJson(
        _$StagedEntityModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'entityType': instance.entityType,
      'entityData': instance.entityData,
      'confidenceScore': instance.confidenceScore,
      'status': instance.status,
      'priority': instance.priority,
      'isPromoted': instance.isPromoted,
      'promotedEntityId': instance.promotedEntityId,
      'sourceDocumentId': instance.sourceDocumentId,
      'questions': instance.questions,
      'createdAt': instance.createdAt,
      'reviewedAt': instance.reviewedAt,
    };
